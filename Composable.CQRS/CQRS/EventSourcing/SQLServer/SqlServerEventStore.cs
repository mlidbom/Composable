using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Security.Cryptography;
using System.Transactions;
using Composable.CQRS.EventSourcing.EventRefactoring;
using Composable.System;
using Composable.System.Reflection;
using Newtonsoft.Json;
using log4net;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    public partial class SqlServerEventStore : IEventStore
    {        
        private static readonly ILog Log = LogManager.GetLogger(typeof(SqlServerEventStore));

        public static readonly JsonSerializerSettings JsonSettings = NewtonSoft.JsonSettings.JsonSerializerSettings;
        public readonly string ConnectionString;

        private readonly SqlServerEventStoreEventsCache _cache;
        private readonly IEventNameMapper _eventNameMapper;

        public SqlServerEventStore(string connectionString, IEventNameMapper nameMapper = null)
        {
            Log.Debug("Constructor called");

            ConnectionString = connectionString;
            _eventNameMapper = nameMapper ?? new DefaultEventNameMapper();
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


        private static string EventSelectClauseForTop(int top)
        {
            return "SELECT TOP {0} EventType, Event, AggregateId, AggregateVersion, EventId, TimeStamp FROM Events With(UPDLOCK,READCOMMITTED, ROWLOCK) ".FormatWith(top);
        }

        private const string EventSelectClause = "SELECT EventType, Event, AggregateId, AggregateVersion, EventId, TimeStamp FROM Events With(UPDLOCK,READCOMMITTED, ROWLOCK) ";
        public IEnumerable<IAggregateRootEvent> GetAggregateHistory(Guid aggregateId)
        {            
            EnsureEventsTableExists();
            var result = _cache.Get(aggregateId);

            using (var connection = OpenSession(suppressTransactionWarning:true))
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
                        _cache.Store(aggregateId, result);
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

        public const int StreamEventsAfterEventWithIdBatchSize = 10000;
       
        public IEnumerable<IAggregateRootEvent> StreamEventsAfterEventWithId(Guid? startAfterEventId)
        {
            EnsureEventsTableExists();

            using (var connection = OpenSession())
            {
                var done = false;
                while(!done)
                {
                    using(var loadCommand = connection.CreateCommand())
                    {
                        if(startAfterEventId.HasValue)
                        {
                            loadCommand.CommandText = EventSelectClauseForTop(StreamEventsAfterEventWithIdBatchSize) + "WHERE SqlTimeStamp > @TimeStamp ORDER BY SqlTimeStamp ASC";
                            loadCommand.Parameters.Add(new SqlParameter("TimeStamp", new SqlBinary(GetEventTimestamp(startAfterEventId.Value))));
                        }
                        else
                        {
                            loadCommand.CommandText = EventSelectClauseForTop(StreamEventsAfterEventWithIdBatchSize) + " ORDER BY SqlTimeStamp ASC";
                        }

                        var fetchedInThisBatch = 0;
                        using(var reader = loadCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var @event = ReadEvent(reader);
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
            return (IAggregateRootEvent)JsonConvert.DeserializeObject(eventData, _eventNameMapper.GetType(eventType), JsonSettings);
        }


        private readonly HashSet<Guid> _aggregatesWithEventsAddedByThisInstance = new HashSet<Guid>(); 
        public void SaveEvents(IEnumerable<IAggregateRootEvent> events)
        {
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
                        command.Parameters.Add(new SqlParameter("EventType", _eventNameMapper.GetName(@event.GetType())));
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
            EnsureEventsTableExists();

            _cache.Remove(aggregateId);
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

        public IEnumerable<Guid> StreamAggregateIdsInCreationOrder()
        {
            EnsureEventsTableExists();

            using (var connection = OpenSession())
            {
                using (var loadCommand = connection.CreateCommand())
                {
                    loadCommand.CommandText = "SELECT AggregateId FROM Events WHERE AggregateVersion = 1 ORDER BY SqlTimeStamp ASC";

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

        private static readonly HashSet<string> VerifiedTables = new HashSet<string>();        

        //todo:Move this and its cousins below into another abstraction, delegate to that abstraction, and obsolete these methods.
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
CREATE UNIQUE NONCLUSTERED INDEX [SqlTimeStamp] ON [dbo].[Events]
(
	[SqlTimeStamp] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
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
            using (var session = new SqlServerEventStore(connectionString))
            {
                session.ResetDB();
            }
        }

        public void ResetDB()
        {
            _cache.Clear();
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
        }
    }
}