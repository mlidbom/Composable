using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Transactions;
using Composable.System.Linq;
using log4net;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    public class SqlServerEventStore : IEventStore
    {               
        private static readonly ILog Log = LogManager.GetLogger(typeof(SqlServerEventStore));

        private static readonly SqlServerEvestStoreEventSerializer EventSerializer = new SqlServerEvestStoreEventSerializer();

        public readonly string ConnectionString;
        private SqlServerEventStoreEventReader EventReader { get; } = new SqlServerEventStoreEventReader();

        private readonly SqlServerEventStoreEventsCache _cache;
        private readonly SqlServerEventStoreSchemaManager _schemaManager;
        public SqlServerEventStore(string connectionString)
        {
            Log.Debug("Constructor called");
            ConnectionString = connectionString;
            _schemaManager =  new SqlServerEventStoreSchemaManager(connectionString);
            _cache = SqlServerEventStoreEventsCache.ForConnectionString(connectionString);
        }

        private SqlConnection OpenSession(bool suppressTransactionWarning = false)
        {
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            if(!suppressTransactionWarning && Transaction.Current == null)
            {
                Log.Warn("No ambient transaction. This is dangerous");
            }
            return connection;
        }


        public IEnumerable<IAggregateRootEvent> GetAggregateHistory(Guid aggregateId)
        {
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();
            var cachedAggregateHistory = _cache.Get(aggregateId);

            using (var connection = OpenSession(suppressTransactionWarning:true))
            {
                using(var loadCommand = connection.CreateCommand())
                {
                    loadCommand.CommandText = EventReader.SelectClause + $"WHERE {EventTable.Columns.AggregateId} = @{EventTable.Columns.AggregateId}";
                    loadCommand.Parameters.Add(new SqlParameter($"{EventTable.Columns.AggregateId}", aggregateId));

                    if (cachedAggregateHistory.Any())
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
                    if (!_aggregatesWithEventsAddedByThisInstance.Contains(aggregateId))
                    {
                        _cache.Store(aggregateId, cachedAggregateHistory);
                    }
                    return cachedAggregateHistory;
                }
            }
        }

        private byte[] GetEventTimestamp(Guid eventId)
        {
            using (var connection = OpenSession())
            {
                using (var loadCommand = connection.CreateCommand())
                {
                    loadCommand.CommandText = $"SELECT {EventTable.Columns.SqlTimeStamp} FROM {EventTable.Name} WHERE {EventTable.Columns.EventId} = @{EventTable.Columns.EventId}";
                    loadCommand.Parameters.Add(new SqlParameter(EventTable.Columns.EventId, eventId));
                    return (byte[]) loadCommand.ExecuteScalar();
                }
            }
        }

        public const int StreamEventsAfterEventWithIdBatchSize = 10000;
       
        public IEnumerable<IAggregateRootEvent> StreamEventsAfterEventWithId(Guid? startAfterEventId)
        {
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();

            using (var connection = OpenSession())
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
                                yield return @event;
                                fetchedInThisBatch++;
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
            using (var connection = OpenSession())
            {
                foreach (var @event in events)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;

                        command.CommandText +=
                            $@"
INSERT {EventTable.Name} With(READCOMMITTED, ROWLOCK) 
       ({EventTable.Columns.AggregateId},  {EventTable.Columns.AggregateVersion},  {EventTable.Columns.EventType},  {EventTable.Columns.EventId},  {EventTable.Columns.TimeStamp},  {EventTable.Columns.Event}) 
VALUES(@{EventTable.Columns.AggregateId}, @{EventTable.Columns.AggregateVersion}, @{EventTable.Columns.EventType}, @{EventTable.Columns.EventId}, @{EventTable.Columns.TimeStamp}, @{EventTable.Columns.Event})";

                        command.Parameters.Add(new SqlParameter(EventTable.Columns.AggregateId, @event.AggregateRootId));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.AggregateVersion, @event.AggregateRootVersion));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.EventType, @event.GetType().FullName));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.EventId, @event.EventId));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.TimeStamp, @event.TimeStamp));

                        command.Parameters.Add(new SqlParameter(EventTable.Columns.Event, EventSerializer.Serialize(@event)));

                        command.ExecuteNonQuery();
                    }
                }
            }
        }       

        public void DeleteEvents(Guid aggregateId)
        {
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();

            _cache.Remove(aggregateId);
            using (var connection = OpenSession())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText += $"DELETE {EventTable.Name} With(ROWLOCK) WHERE {EventTable.Columns.AggregateId} = @{EventTable.Columns.AggregateId}";
                    command.Parameters.Add(new SqlParameter(EventTable.Columns.AggregateId, aggregateId));
                    command.ExecuteNonQuery();
                }
            }
        }

        public IEnumerable<Guid> StreamAggregateIdsInCreationOrder()
        {
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();

            using (var connection = OpenSession())
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