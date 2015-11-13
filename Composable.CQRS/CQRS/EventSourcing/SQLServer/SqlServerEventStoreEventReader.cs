using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    internal class SqlServerEventStoreEventReader
    {
        private readonly SqlServerEventStoreConnectionManager _connectionMananger;
        private readonly SqlServerEventStoreSchemaManager _schemaManager;
        public IEventTypeToIdMapper EventTypeToIdMapper => _schemaManager.IdMapper;

        public string SelectClause => InternalSelect();
        public string SelectTopClause(int top) => InternalSelect(top);

        private string InternalSelect(int? top = null)
        {
            var topClause = top.HasValue ? $"TOP {top.Value} " : "";
            return $@"
SELECT {topClause} {EventTable.Columns.EventType}, {EventTable.Columns.Event}, {EventTable.Columns.AggregateId}, {EventTable.Columns.EffectiveVersion}, {EventTable.Columns.EventId}, {EventTable.Columns.TimeStamp}, {EventTable.Columns.InsertionOrder}, {EventTable.Columns.InsertAfter}, {EventTable.Columns.InsertBefore}, {EventTable.Columns.Replaces}, {EventTable.Columns.InsertedVersion}, {EventTable.Columns.ManualVersion}
FROM {EventTable.Name} With(UPDLOCK, READCOMMITTED, ROWLOCK) ";
        }

        private static readonly SqlServerEvestStoreEventSerializer EventSerializer = new SqlServerEvestStoreEventSerializer();

        public SqlServerEventStoreEventReader(SqlServerEventStoreConnectionManager connectionManager, SqlServerEventStoreSchemaManager schemaManager)
        {
            _connectionMananger = connectionManager;
            _schemaManager = schemaManager;
        }

        private IAggregateRootEvent Read(SqlDataReader eventReader)
        {
            var @event = (AggregateRootEvent)EventSerializer.Deserialize( eventType: EventTypeToIdMapper.GetType(eventReader.GetInt32(0)) , eventData: eventReader.GetString(1));
            @event.AggregateRootId = eventReader.GetGuid(2);
            @event.AggregateRootVersion = eventReader[3] as int? ?? eventReader.GetInt32(10);
            @event.EventId = eventReader.GetGuid(4);
            @event.TimeStamp = eventReader.GetDateTime(5);
            @event.InsertionOrder = eventReader.GetInt64(6);
            @event.InsertAfter = eventReader[7] as long?;
            @event.InsertBefore = eventReader[8] as long?;
            @event.Replaces = eventReader[9] as long?;
            @event.InsertedVersion = eventReader.GetInt32(10);
            @event.ManualVersion = eventReader[11] as int?;

            return @event;
        }

        public IEnumerable<IAggregateRootEvent> GetAggregateHistory(Guid aggregateId, int startAfterVersion = 0, bool suppressTransactionWarning = false)
        {
            using(var connection = _connectionMananger.OpenConnection(suppressTransactionWarning: suppressTransactionWarning))
            {
                using (var loadCommand = connection.CreateCommand()) 
                {
                    loadCommand.CommandText = $"{SelectClause} WHERE {EventTable.Columns.AggregateId} = @{EventTable.Columns.AggregateId}";
                    loadCommand.Parameters.Add(new SqlParameter($"{EventTable.Columns.AggregateId}", aggregateId));

                    if (startAfterVersion > 0)
                    {
                        loadCommand.CommandText += $" AND {EventTable.Columns.InsertedVersion} > @CachedVersion";
                        loadCommand.Parameters.Add(new SqlParameter("CachedVersion", startAfterVersion));
                    }

                    loadCommand.CommandText += $" ORDER BY {EventTable.Columns.InsertedVersion} ASC";

                    using (var reader = loadCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return Read(reader);
                        }
                    }
                }
            }
        }

        public IEnumerable<IAggregateRootEvent> StreamEvents(int batchSize)
        {
            using(var connection = _connectionMananger.OpenConnection())
            {
                var done = false;
                while(!done)
                {
                    using(var loadCommand = connection.CreateCommand())
                    {

                        loadCommand.CommandText = SelectTopClause(batchSize) + InsertionOrderSortOrder;

                        var fetchedInThisBatch = 0;
                        using(var reader = loadCommand.ExecuteReader())
                        {
                            while(reader.Read())
                            {
                                var @event = Read(reader);
                                fetchedInThisBatch++;
                                yield return @event;
                            }
                        }
                        done = fetchedInThisBatch < batchSize;
                    }
                }
            }
        }

        private object GetEventInsertionOrder(Guid eventId)
        {
            return _connectionMananger.UseCommand(
                loadCommand =>
                {
                    loadCommand.CommandText = $"SELECT {EventTable.Columns.InsertionOrder} FROM {EventTable.Name} WHERE {EventTable.Columns.EventId} = @{EventTable.Columns.EventId}";
                    loadCommand.Parameters.Add(new SqlParameter(EventTable.Columns.EventId, eventId));
                    return loadCommand.ExecuteScalar();
                });
        }

        public IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type eventBaseType = null)
        {
            using (var connection = _connectionMananger.OpenConnection())
            {
                using (var loadCommand = connection.CreateCommand())
                {
                    loadCommand.CommandText = $"SELECT {EventTable.Columns.AggregateId}, {EventTable.Columns.EventType} FROM {EventTable.Name} WHERE {EventTable.Columns.InsertedVersion} = 1 {InsertionOrderSortOrder}";

                    using (var reader = loadCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if(eventBaseType == null || eventBaseType.IsAssignableFrom(EventTypeToIdMapper.GetType(reader.GetInt32(1))))
                            {
                                yield return (Guid)reader[0];
                            }
                        }
                    }
                }
            }
        }


        private string InsertionOrderSortOrder => $" ORDER BY {EventTable.Columns.InsertionOrder} ASC";        
    }
}