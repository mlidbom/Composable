using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using Composable.System.Linq;
using log4net;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    public class SqlServerEventStore : IEventStore
    {               
        private static readonly ILog Log = LogManager.GetLogger(typeof(SqlServerEventStore));

        private readonly SqlServerEvestStoreEventSerializer _eventSerializer = new SqlServerEvestStoreEventSerializer();

        public readonly string ConnectionString;
        private SqlServerEventStoreEventReader EventReader = new SqlServerEventStoreEventReader();
        private readonly SqlServerEventStoreConnectionManager _connectionMananger;
        private readonly SqlServerEventStoreEventWriter _eventWriter;

        private readonly SqlServerEventStoreEventsCache _cache;
        private readonly SqlServerEventStoreSchemaManager _schemaManager;
        public SqlServerEventStore(string connectionString)
        {
            Log.Debug("Constructor called");
            ConnectionString = connectionString;
            _schemaManager =  new SqlServerEventStoreSchemaManager(connectionString);
            _cache = SqlServerEventStoreEventsCache.ForConnectionString(connectionString);
            _connectionMananger = new SqlServerEventStoreConnectionManager(connectionString);
            _eventWriter = new SqlServerEventStoreEventWriter(_connectionMananger, _eventSerializer);
        }


        public IEnumerable<IAggregateRootEvent> GetAggregateHistory(Guid aggregateId)
        {
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();
            var cachedAggregateHistory = _cache.Get(aggregateId);

            _connectionMananger.UseCommand(
                loadCommand =>
                {
                    loadCommand.CommandText = EventReader.SelectClause + $"WHERE {EventTable.Columns.AggregateId} = @{EventTable.Columns.AggregateId}";
                    loadCommand.Parameters.Add(new SqlParameter($"{EventTable.Columns.AggregateId}", aggregateId));

                    if(cachedAggregateHistory.Any())
                    {
                        loadCommand.CommandText += $" AND {EventTable.Columns.AggregateVersion} > @CachedVersion";
                        loadCommand.Parameters.Add(new SqlParameter("CachedVersion", cachedAggregateHistory.Last().AggregateRootVersion));
                    }

                    loadCommand.CommandText += $" ORDER BY {EventTable.Columns.AggregateVersion} ASC";

                    using(var reader = loadCommand.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            cachedAggregateHistory.Add(EventReader.Read(reader));
                        }
                    }
                    //Should within a transaction a process write events, read them, then fail to commit we will have cached events that are not persisted
                    if(!_aggregatesWithEventsAddedByThisInstance.Contains(aggregateId))
                    {
                        _cache.Store(aggregateId, cachedAggregateHistory);
                    }
                }, suppressTransactionWarning: true);
            return cachedAggregateHistory;
        }

        private byte[] GetEventTimestamp(Guid eventId)
        {
            return _connectionMananger.UseCommand(
                loadCommand =>
                {
                    loadCommand.CommandText = $"SELECT {EventTable.Columns.SqlTimeStamp} FROM {EventTable.Name} WHERE {EventTable.Columns.EventId} = @{EventTable.Columns.EventId}";
                    loadCommand.Parameters.Add(new SqlParameter(EventTable.Columns.EventId, eventId));
                    return (byte[])loadCommand.ExecuteScalar();
                });
        }

        public const int StreamEventsAfterEventWithIdBatchSize = 10000;
       
        public IEnumerable<IAggregateRootEvent> StreamEventsAfterEventWithId(Guid? startAfterEventId)
        {
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();

            using (var connection = _connectionMananger.OpenConnection())
            {
                var done = false;
                while(!done)
                {
                    using(var loadCommand = connection.CreateCommand())
                    {
                        if(startAfterEventId.HasValue)
                        {
                            loadCommand.CommandText = EventReader.SelectTopClause(StreamEventsAfterEventWithIdBatchSize) + $"WHERE {EventTable.Columns.SqlTimeStamp} > @{EventTable.Columns.SqlTimeStamp} ORDER BY {EventTable.Columns.SqlTimeStamp} ASC";
                            loadCommand.Parameters.Add(new SqlParameter(EventTable.Columns.SqlTimeStamp, new SqlBinary(GetEventTimestamp(startAfterEventId.Value))));
                        }
                        else
                        {
                            loadCommand.CommandText = EventReader.SelectTopClause(StreamEventsAfterEventWithIdBatchSize) + $" ORDER BY {EventTable.Columns.SqlTimeStamp} ASC";
                        }

                        var fetchedInThisBatch = 0;
                        using(var reader = loadCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var @event = EventReader.Read(reader);
                                startAfterEventId = @event.EventId;
                                fetchedInThisBatch++;
                                yield return @event;                                
                            }
                        }
                        done = fetchedInThisBatch < StreamEventsAfterEventWithIdBatchSize;
                    }
                }
            }
        }


        private readonly HashSet<Guid> _aggregatesWithEventsAddedByThisInstance = new HashSet<Guid>(); 
        public void SaveEvents(IEnumerable<IAggregateRootEvent> events)
        {
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();

            events = events.ToList();
            _aggregatesWithEventsAddedByThisInstance.AddRange(events.Select(e => e.AggregateRootId));
            _eventWriter.Insert(events);
        }

        public void DeleteEvents(Guid aggregateId)
        {
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();
            _cache.Remove(aggregateId);
            _connectionMananger.UseCommand(command =>
                                           {
                                               command.CommandType = CommandType.Text;
                                               command.CommandText +=
                                                   $"DELETE {EventTable.Name} With(ROWLOCK) WHERE {EventTable.Columns.AggregateId} = @{EventTable.Columns.AggregateId}";
                                               command.Parameters.Add(new SqlParameter(EventTable.Columns.AggregateId, aggregateId));
                                               command.ExecuteNonQuery();
                                           });
        }

        public IEnumerable<Guid> StreamAggregateIdsInCreationOrder()
        {
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();

            using (var connection = _connectionMananger.OpenConnection())
            {
                using (var loadCommand = connection.CreateCommand())
                {
                    loadCommand.CommandText = $"SELECT {EventTable.Columns.AggregateId} FROM {EventTable.Name} WHERE {EventTable.Columns.AggregateVersion} = 1 ORDER BY {EventTable.Columns.SqlTimeStamp} ASC";

                    using (var reader = loadCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return (Guid)reader[0];
                        }
                    }
                }
            }
        }        

        public static void ResetDB(string connectionString)
        {
            new SqlServerEventStoreSchemaManager(connectionString).ResetDB();
        }

        public void ResetDB()
        {
            _cache.Clear();
            _schemaManager.ResetDB();           
        }


        public void Dispose()
        {
        }
    }
}