using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Runtime.Caching;
using System.Transactions;
using Composable.System;
using Composable.System.Reflection;
using Composable.SystemExtensions.Threading;
using Newtonsoft.Json;
using log4net;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    public class SqlServerEventStore : IEventStore
    {
 
        private class EventsCache
        {
            //todo: this way of doing cache expiration is unlikely to be acceptable in the long run....
            private static MemoryCache InternalCache = new MemoryCache("eventsCache_06B4FF4E-14A2-498C-8277-02895B81BE72");

            private static readonly CacheItemPolicy Policy = new CacheItemPolicy()
                                                                 {
                                                                     SlidingExpiration = 20.Minutes()
                                                                 };

            public List<IAggregateRootEvent> Get(Guid id)
            {
                var cached = InternalCache.Get(id.ToString());
                if(cached == null)
                {
                    return new List<IAggregateRootEvent>();
                }
                //Make sure each caller gets their own copy.
                return ((List<IAggregateRootEvent>)cached).ToList();

            }

            public void Store(Guid id, IEnumerable<IAggregateRootEvent> events)
            {
             
                InternalCache.Set(key: id.ToString(), policy: Policy, value: events.ToList());
            }
            public void Clear()
            {
                InternalCache.Dispose();
                InternalCache = new MemoryCache("name");
            }

            public void Remove(Guid id)
            {
                InternalCache.Remove(key: id.ToString());
            }
        }

        private static readonly EventsCache cache = new EventsCache();

        private static readonly ILog Log = LogManager.GetLogger(typeof(SqlServerEventStore));

        public readonly ISingleContextUseGuard UsageGuard;

        public static readonly JsonSerializerSettings JsonSettings = NewtonSoft.JsonSettings.JsonSerializerSettings;
        public readonly string ConnectionString;

        public SqlServerEventStore(ISingleContextUseGuard usageGuard, string connectionString)
        {
            Log.Debug("Constructor called");
            UsageGuard = usageGuard;           
            ConnectionString = connectionString;
        }

        private SqlConnection OpenSession()
        {
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            if(Transaction.Current == null)
            {
                Log.Warn("No ambient transaction. This is dangerous");
            }
            return connection;
        }


        private const string EventSelectClause = "SELECT EventType, Event, AggregateId, AggregateVersion, EventId, TimeStamp FROM Events With(READCOMMITTED, ROWLOCK) ";
        public IEnumerable<IAggregateRootEvent> GetHistoryUnSafe(Guid aggregateId)
        {            
            UsageGuard.AssertNoContextChangeOccurred(this);
            EnsureEventsTableExists();
            var result = cache.Get(aggregateId);

            using (var connection = OpenSession())
            {
                using(var loadCommand = connection.CreateCommand())
                {
                    loadCommand.CommandText = EventSelectClause + "WHERE AggregateId = @AggregateId";
                    loadCommand.Parameters.Add(new SqlParameter("AggregateId", aggregateId));

                    if (result.Any())
                    {
                        loadCommand.CommandText += " AND AggregateVersion > @CachedVersion";
                        loadCommand.Parameters.Add(new SqlParameter("CachedVersion", result.Last().AggregateRootVersion));
                    }

                    loadCommand.CommandText += " ORDER BY AggregateVersion ASC";

                    using(var reader = loadCommand.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            result.Add(ReadEvent(reader));
                        }
                    }
                    //Should within a transaction a process write events, read them, then fail to commit we will have cached events that are not persisted
                    if (!_aggregatesWithEventsAddedByThisInstance.Contains(aggregateId))
                    {
                        cache.Store(aggregateId, result);
                    }
                    return result;
                }
            }
        }

        private Byte[] GetEventTimestamp(Guid eventId)
        {
            using (var connection = OpenSession())
            {
                using (var loadCommand = connection.CreateCommand())
                {
                    loadCommand.CommandText = "SELECT SqlTimeStamp FROM Events WHERE EventId = @EventId";
                    loadCommand.Parameters.Add(new SqlParameter("EventId", eventId));
                    return (byte[]) loadCommand.ExecuteScalar();
                }
            }
        }

        public IEnumerable<IAggregateRootEvent> StreamEventsAfterEventWithId(Guid? startAfterEventId)
        {
            UsageGuard.AssertNoContextChangeOccurred(this);
            EnsureEventsTableExists();

            using (var connection = OpenSession())
            {
                using (var loadCommand = connection.CreateCommand())
                {
                    if (startAfterEventId.HasValue)
                    {
                        loadCommand.CommandText = EventSelectClause + "WHERE SqlTimeStamp > @TimeStamp ORDER BY SqlTimeStamp ASC";
                        loadCommand.Parameters.Add(new SqlParameter("TimeStamp", new SqlBinary(GetEventTimestamp(startAfterEventId.Value))));
                    }else
                    {
                        loadCommand.CommandText = EventSelectClause + " ORDER BY SqlTimeStamp ASC";
                    }

                    using (var reader = loadCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return ReadEvent(reader);
                        }
                    }
                }
            }
        }

        private IAggregateRootEvent ReadEvent(SqlDataReader eventReader)
        {
            var @event = DeserializeEvent(eventReader.GetString(0), eventReader.GetString(1));
            @event.AggregateRootId = eventReader.GetGuid(2);
            @event.AggregateRootVersion = eventReader.GetInt32(3);
            @event.EventId = eventReader.GetGuid(4);
            @event.TimeStamp = eventReader.GetDateTime(5);

            return @event;
        }

        private IAggregateRootEvent DeserializeEvent(string eventType, string eventData)
        {
            return (IAggregateRootEvent)JsonConvert.DeserializeObject(eventData, eventType.AsType(), JsonSettings);
        }


        private readonly HashSet<Guid> _aggregatesWithEventsAddedByThisInstance = new HashSet<Guid>(); 
        public void SaveEvents(IEnumerable<IAggregateRootEvent> events)
        {
            UsageGuard.AssertNoContextChangeOccurred(this);
            EnsureEventsTableExists();

            events = events.ToList();
            _aggregatesWithEventsAddedByThisInstance.AddRange(events.Select(e => e.AggregateRootId));
            using (var connection = OpenSession())
            {
                foreach (var @event in events)
                {
                    using(var command = connection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;

                        command.CommandText += "INSERT Events With(READCOMMITTED, ROWLOCK) (AggregateId, AggregateVersion, EventType, EventId, TimeStamp, Event) VALUES(@AggregateId, @AggregateVersion, @EventType, @EventId, @TimeStamp, @Event)";

                        command.Parameters.Add(new SqlParameter("AggregateId", @event.AggregateRootId));
                        command.Parameters.Add(new SqlParameter("AggregateVersion", @event.AggregateRootVersion));
                        command.Parameters.Add(new SqlParameter("EventType", @event.GetType().FullName));
                        command.Parameters.Add(new SqlParameter("EventId", @event.EventId));
                        command.Parameters.Add(new SqlParameter("TimeStamp", @event.TimeStamp));

                        command.Parameters.Add(new SqlParameter("Event", JsonConvert.SerializeObject(@event, Formatting.Indented, JsonSettings)));

                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public void DeleteEvents(Guid aggregateId)
        {
            UsageGuard.AssertNoContextChangeOccurred(this);
            EnsureEventsTableExists();

            cache.Remove(aggregateId);
            using (var connection = OpenSession())
            {
                using(var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText += "DELETE Events With(ROWLOCK) WHERE AggregateId = @AggregateId";
                    command.Parameters.Add(new SqlParameter("AggregateId", aggregateId));
                    command.ExecuteNonQuery();
                }
            }
        }

        public IEnumerable<Guid> GetAggregateIds()
        {
            UsageGuard.AssertNoContextChangeOccurred(this);
            EnsureEventsTableExists();

            using (var connection = OpenSession())
            {
                using (var loadCommand = connection.CreateCommand())
                {
                    loadCommand.CommandText = EventSelectClause + "WHERE AggregateVersion = 1";

                    using (var reader = loadCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return (Guid)reader[2];
                        }
                    }
                }
            }
        }

        private static readonly HashSet<string> VerifiedTables = new HashSet<string>();        

        private void EnsureEventsTableExists()
        {
            lock (VerifiedTables)
            {
                if (!VerifiedTables.Contains(ConnectionString))
                {
                    int exists;
                    using (var _connection = OpenSession())
                    {
                        using (var checkForTableCommand = _connection.CreateCommand())
                        {
                            checkForTableCommand.CommandText = "select count(*) from sys.tables where name = 'Events'";
                            exists = (int)checkForTableCommand.ExecuteScalar();
                        }
                        if (exists == 0)
                        {
                            using (var createTableCommand = _connection.CreateCommand())
                            {
                                createTableCommand.CommandText =
                                    @"
CREATE TABLE [dbo].[Events](
	[AggregateId] [uniqueidentifier] NOT NULL,
	[AggregateVersion] [int] NOT NULL,
	[TimeStamp] [datetime] NOT NULL,
	[SqlTimeStamp] [timestamp] NOT NULL,
	[EventType] [varchar](300) NOT NULL,
	[EventId] [uniqueidentifier] NOT NULL,
	[Event] [nvarchar](max) NOT NULL,
CONSTRAINT [IX_Uniq_EventId] UNIQUE
(
	EventId
),
CONSTRAINT [PK_Events] PRIMARY KEY CLUSTERED 
(
	[AggregateId] ASC,
	[AggregateVersion] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF) ON [PRIMARY]
) ON [PRIMARY]
";
                                createTableCommand.ExecuteNonQuery();
                            }
                        }
                        VerifiedTables.Add(ConnectionString);
                    }
                }
            }
        }

        public static void ResetDB(string connectionString)
        {
            using (var session = new SqlServerEventStore(new SingleThreadUseGuard(), connectionString))
            {
                session.ResetDB();
            }
        }

        public void ResetDB()
        {
            UsageGuard.AssertNoContextChangeOccurred(this);
            cache.Clear();
            using (var connection = OpenSession())
            {
                using(var dropCommand = connection.CreateCommand())
                {
                    dropCommand.CommandText =
                        @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Events]') AND type in (N'U'))
DROP TABLE [dbo].[Events]";

                    dropCommand.ExecuteNonQuery();
                    lock (VerifiedTables)
                    {
                        VerifiedTables.Remove(ConnectionString);
                    }                    
                }
            }
            EnsureEventsTableExists();
        }


        public void Dispose()
        {
            UsageGuard.AssertNoContextChangeOccurred(this);
        }
    }
}