using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Composable.Persistence.EventStore;

namespace Composable.Persistence.SqlServer.EventStore
{
    class SqlServerEventStoreEventReader : IEventStoreEventReader
    {
        readonly SqlServerEventStoreConnectionManager _connectionManager;
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
    {SqlServerEventTable.Columns.EventType}, 
    {SqlServerEventTable.Columns.Event}, 
    {SqlServerEventTable.Columns.AggregateId}, 
    {SqlServerEventTable.Columns.EffectiveVersion}, 
    {SqlServerEventTable.Columns.EventId}, 
    {SqlServerEventTable.Columns.UtcTimeStamp}, 
    {SqlServerEventTable.Columns.InsertionOrder}, 
    {SqlServerEventTable.Columns.InsertAfter}, 
    {SqlServerEventTable.Columns.InsertBefore}, 
    {SqlServerEventTable.Columns.Replaces}, 
    {SqlServerEventTable.Columns.InsertedVersion}, 
    {SqlServerEventTable.Columns.ManualVersion}, 
    {SqlServerEventTable.Columns.EffectiveReadOrder}
FROM {SqlServerEventTable.Name} {lockHint} ";
        }

        public SqlServerEventStoreEventReader(SqlServerEventStoreConnectionManager connectionManager, IEventStoreSchemaManager schemaManager)
        {
            _connectionManager = connectionManager;
            _schemaManager = schemaManager;
        }

        static EventReadDataRow ReadDataRow(SqlDataReader eventReader) => new EventReadDataRow(
            eventType: eventReader.GetInt32(0),
            eventJson: eventReader.GetString(1),
            eventId: eventReader.GetGuid(4),
            aggregateVersion: eventReader[3] as int? ?? eventReader.GetInt32(10),
            aggregateId: eventReader.GetGuid(2),
            //Without this the datetime will be DateTimeKind.Unspecified and will not convert correctly into Local time....
            utcTimeStamp: DateTime.SpecifyKind(eventReader.GetDateTime(5), DateTimeKind.Utc),
            insertionOrder: eventReader.GetInt64(6),
            insertAfter: eventReader[7] as long?,
            insertBefore: eventReader[8] as long?,
            replaces: eventReader[9] as long?,
            insertedVersion: eventReader.GetInt32(10),
            manualVersion: eventReader[11] as int?,
            effectiveVersion: eventReader[3] as int?
        );

        public IReadOnlyList<EventReadDataRow> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0)
        {
            var historyData = new List<EventReadDataRow>();
            using (var connection = _connectionManager.OpenConnection(suppressTransactionWarning: !takeWriteLock))
            {
                using var loadCommand = connection.CreateCommand();
                loadCommand.CommandText = $"{GetSelectClause(takeWriteLock)} WHERE {SqlServerEventTable.Columns.AggregateId} = @{SqlServerEventTable.Columns.AggregateId}";
                loadCommand.Parameters.Add(new SqlParameter($"{SqlServerEventTable.Columns.AggregateId}", SqlDbType.UniqueIdentifier) {Value = aggregateId});

                if (startAfterInsertedVersion > 0)
                {
                    loadCommand.CommandText += $" AND {SqlServerEventTable.Columns.InsertedVersion} > @CachedVersion";
                    loadCommand.Parameters.Add(new SqlParameter("CachedVersion", SqlDbType.Int) {Value = startAfterInsertedVersion});
                }

                loadCommand.CommandText += $" ORDER BY {SqlServerEventTable.Columns.EffectiveReadOrder} ASC";

                using var reader = loadCommand.ExecuteReader();
                while (reader.Read())
                {
                    var eventDataRow = ReadDataRow(reader);
                    if (eventDataRow.EffectiveVersion > 0)
                    {
                        historyData.Add(eventDataRow);
                    }
                }
            }

            return historyData;
        }

        public IEnumerable<EventReadDataRow> StreamEvents(int batchSize)
        {
            SqlDecimal lastReadEventReadOrder = 0;
            using var connection = _connectionManager.OpenConnection(suppressTransactionWarning: true);
            var done = false;
            while (!done)
            {
                var historyData = new List<EventReadDataRow>();
                using (var loadCommand = connection.CreateCommand())
                {

                    loadCommand.CommandText = SelectTopClause(batchSize, takeWriteLock: false) + $"WHERE {SqlServerEventTable.Columns.EffectiveReadOrder} > 0 AND {SqlServerEventTable.Columns.EffectiveReadOrder}  > @{SqlServerEventTable.Columns.EffectiveReadOrder}" + ReadSortOrder;

                    loadCommand.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.EffectiveReadOrder, SqlDbType.Decimal) {Value = lastReadEventReadOrder});

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

        public IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type? eventBaseType = null)
        {
            var ids = new List<Guid>();
            using (var connection = _connectionManager.OpenConnection(suppressTransactionWarning:true))
            {
                using var loadCommand = connection.CreateCommand();
                loadCommand.CommandText = $"SELECT {SqlServerEventTable.Columns.AggregateId}, {SqlServerEventTable.Columns.EventType} FROM {SqlServerEventTable.Name} WHERE {SqlServerEventTable.Columns.EffectiveVersion} = 1 AND {SqlServerEventTable.Columns.EffectiveReadOrder} > 0 {ReadSortOrder}";

                using var reader = loadCommand.ExecuteReader();
                while (reader.Read())
                {
                    if(eventBaseType == null || eventBaseType.IsAssignableFrom(EventTypeToIdMapper.GetType(reader.GetInt32(1))))
                    {
                        ids.Add((Guid)reader[0]);
                    }
                }
            }
            return ids;
        }

        static string ReadSortOrder => $" ORDER BY {SqlServerEventTable.Columns.EffectiveReadOrder} ASC";
    }
}