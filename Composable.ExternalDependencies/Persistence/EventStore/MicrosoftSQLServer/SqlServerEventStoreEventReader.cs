using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlTypes;

namespace Composable.Persistence.EventStore.MicrosoftSQLServer
{
    class SqlServerEventStoreEventReader : IEventStoreEventReader
    {
        readonly SqlServerEventStoreConnectionManager _connectionMananger;
        readonly IEventStoreSchemaManager _schemaManager;
        IEventTypeToIdMapper EventTypeToIdMapper => _schemaManager.IdMapper;

        static string GetSelectClause(bool takeWriteLock) => InternalSelect(takeWriteLock: takeWriteLock);
        static string SelectTopClause(int top, bool takeWriteLock) => InternalSelect(top: top, takeWriteLock: takeWriteLock);

        static string InternalSelect(bool takeWriteLock, int? top = null)
        {
            var topClause = top.HasValue ? $"TOP {top.Value} " : "";
            var lockHint = takeWriteLock ? "With(UPDLOCK, READCOMMITTED, ROWLOCK)" : "With(READCOMMITTED, ROWLOCK)";

            return $@"
SELECT {topClause} 
    {EventTable.Columns.EventType}, 
    {EventTable.Columns.Event}, 
    {EventTable.Columns.AggregateId}, 
    {EventTable.Columns.EffectiveVersion}, 
    {EventTable.Columns.EventId}, 
    {EventTable.Columns.UtcTimeStamp}, 
    {EventTable.Columns.InsertionOrder}, 
    {EventTable.Columns.InsertAfter}, 
    {EventTable.Columns.InsertBefore}, 
    {EventTable.Columns.Replaces}, 
    {EventTable.Columns.InsertedVersion}, 
    {EventTable.Columns.ManualVersion}, 
    {EventTable.Columns.EffectiveReadOrder}
FROM {EventTable.Name} {lockHint} ";
        }

        public SqlServerEventStoreEventReader(SqlServerEventStoreConnectionManager connectionManager, IEventStoreSchemaManager schemaManager)
        {
            _connectionMananger = connectionManager;
            _schemaManager = schemaManager;
        }

        static EventReadDataRow ReadDataRow(SqlDataReader eventReader) => new EventReadDataRow
                                                                      {
                                                                          EventJson = eventReader.GetString(1),
                                                                          EventType = eventReader.GetInt32(0),
                                                                          AggregateRootId = eventReader.GetGuid(2),
                                                                          AggregateRootVersion = eventReader[3] as int? ?? eventReader.GetInt32(10),
                                                                          EventId = eventReader.GetGuid(4),
                                                                          //Without this the datetime will be DateTimeKind.Unspecified and will not convert correctly into Local time....
                                                                          UtcTimeStamp = DateTime.SpecifyKind(eventReader.GetDateTime(5), DateTimeKind.Utc),
                                                                          InsertionOrder = eventReader.GetInt64(6),
                                                                          InsertAfter = eventReader[7] as long?,
                                                                          InsertBefore = eventReader[8] as long?,
                                                                          Replaces = eventReader[9] as long?,
                                                                          InsertedVersion = eventReader.GetInt32(10),
                                                                          ManualVersion = eventReader[11] as int?,
                                                                          EffectiveVersion = eventReader[3] as int?
                                                                      };

        public IReadOnlyList<EventReadDataRow> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0)
        {
            var historyData = new List<EventReadDataRow>();
            using (var connection = _connectionMananger.OpenConnection(suppressTransactionWarning: !takeWriteLock))
            {
                using (var loadCommand = connection.CreateCommand())
                {
                    loadCommand.CommandText = $"{GetSelectClause(takeWriteLock)} WHERE {EventTable.Columns.AggregateId} = @{EventTable.Columns.AggregateId}";
                    loadCommand.Parameters.Add(new SqlParameter($"{EventTable.Columns.AggregateId}", aggregateId));

                    if (startAfterInsertedVersion > 0)
                    {
                        loadCommand.CommandText += $" AND {EventTable.Columns.InsertedVersion} > @CachedVersion";
                        loadCommand.Parameters.Add(new SqlParameter("CachedVersion", startAfterInsertedVersion));
                    }

                    loadCommand.CommandText += $" ORDER BY {EventTable.Columns.EffectiveReadOrder} ASC";

                    using (var reader = loadCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var eventDataRow = ReadDataRow(reader);
                            if (eventDataRow.EffectiveVersion > 0)
                            {
                                historyData.Add(eventDataRow);
                            }
                        }
                    }
                }
            }

            return historyData;
        }

        public IEnumerable<EventReadDataRow> StreamEvents(int batchSize)
        {
            SqlDecimal lastReadEventReadOrder = 0;
            using (var connection = _connectionMananger.OpenConnection())
            {
                var done = false;
                while (!done)
                {
                    var historyData = new List<EventReadDataRow>();
                    using (var loadCommand = connection.CreateCommand())
                    {

                        loadCommand.CommandText = SelectTopClause(batchSize, takeWriteLock: false) + $"WHERE {EventTable.Columns.EffectiveReadOrder} > 0 AND {EventTable.Columns.EffectiveReadOrder}  > @{EventTable.Columns.EffectiveReadOrder}" + ReadSortOrder;

                        loadCommand.Parameters.Add(new SqlParameter(EventTable.Columns.EffectiveReadOrder, lastReadEventReadOrder));

                        var fetchedInThisBatch = 0;
                        using (var reader = loadCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                historyData.Add(ReadDataRow(reader));
                                fetchedInThisBatch++;
                                lastReadEventReadOrder = reader.GetSqlDecimal(12);
                            }
                        }
                        done = fetchedInThisBatch < batchSize;
                    }

                    //We do not yield while reading from the reader since that may cause code to run that will cause another sql call into the same connection. Something that throws an exception unless you use an unusual and non-recommended connection string setting.
                    foreach (var eventDataRow in historyData)
                    {
                        yield return eventDataRow;
                    }
                }
            }
        }

        public IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type eventBaseType = null)
        {
            var ids = new List<Guid>();
            using (var connection = _connectionMananger.OpenConnection(suppressTransactionWarning:true))
            {
                using (var loadCommand = connection.CreateCommand())
                {
                    loadCommand.CommandText = $"SELECT {EventTable.Columns.AggregateId}, {EventTable.Columns.EventType} FROM {EventTable.Name} WHERE {EventTable.Columns.EffectiveVersion} = 1 AND {EventTable.Columns.EffectiveReadOrder} > 0 {ReadSortOrder}";

                    using (var reader = loadCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if(eventBaseType == null || eventBaseType.IsAssignableFrom(EventTypeToIdMapper.GetType(reader.GetInt32(1))))
                            {
                                ids.Add((Guid)reader[0]);
                            }
                        }
                    }
                }
            }
            return ids;
        }

        static string ReadSortOrder => $" ORDER BY {EventTable.Columns.EffectiveReadOrder} ASC";
    }
}