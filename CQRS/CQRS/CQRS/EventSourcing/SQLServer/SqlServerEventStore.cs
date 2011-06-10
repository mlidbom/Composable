#region usings

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using Composable.NewtonSoft;
using Composable.ServiceBus;
using Composable.System;
using Composable.System.Linq;
using log4net;
using log4net.Core;
using Newtonsoft.Json;
using Composable.System.Reflection;

#endregion

namespace Composable.CQRS.EventSourcing.SQLServer
{
    [Flags]
    public enum SqlServerEventStoreConfig
    {
        Default = 0x0,
        NoBatching = 0x2  
    }

    public class SqlServerEventStore : IEventStore
    {
        public string ConnectionString { get; private set; }
        public SqlServerEventStoreConfig Config {get;private set;}
        public IServiceBus Bus { get; private set; }

        public SqlServerEventStore(string connectionString, IServiceBus bus, SqlServerEventStoreConfig config = SqlServerEventStoreConfig.Default)
        {
            ConnectionString = connectionString;
            Bus = bus;
            Config = config;
        }

        public IEventStoreSession OpenSession()
        {
            return new SqlServerEventStoreSession(this);
        }

        public static void ResetDB(string connectionString)
        {
            var me = new SqlServerEventStore(connectionString, null);
            using(var session = new SqlServerEventStoreSession(me))
            {
                session.ResetDB();
            }
        }
    }

    public class SqlServerEventStoreSession : EventStoreSession
    {
        private static ILog Log = LogManager.GetLogger(typeof(SqlServerEventStoreSession));
        private static readonly HashSet<String> VerifiedTables = new HashSet<String>();
        private bool EventsTableVerifiedToExist
        {
            get
            {
                return VerifiedTables.Contains(_store.ConnectionString);
            }

            set
            {
                if (value)
                {
                    if (!EventsTableVerifiedToExist)
                    {
                        VerifiedTables.Add(_store.ConnectionString);
                    }
                }
                else if (EventsTableVerifiedToExist)
                {
                    VerifiedTables.Remove(_store.ConnectionString);
                }
            }
        }


        private readonly SqlServerEventStore _store;
        private int SqlBatchSize = 10;

        private readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
                                                                   {
                                                                       TypeNameHandling = TypeNameHandling.Auto,
                                                                       ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                                                                       ContractResolver = new IncludeMembersWithPrivateSettersResolver()
                                                                   };

        public SqlServerEventStoreSession(SqlServerEventStore store) : base(store.Bus)
        {
            Log.Debug("Constructor called");
            //Console.WriteLine("{0}: {1}", GetType().Name, ++instances);
            _store = store;
            EnsureEventsTableExists();            

            if(_store.Config.HasFlag(SqlServerEventStoreConfig.NoBatching))
            {
                SqlBatchSize = 1;
            }
        }

        private SqlConnection OpenSession()
        {
            var connection = new SqlConnection(_store.ConnectionString);
            connection.Open();
            return connection;
        }

        private void EnsureEventsTableExists()
        {
            if(!EventsTableVerifiedToExist)
            {
                int exists;
                using (var _connection = OpenSession())
                {
                    using(var checkForTableCommand = _connection.CreateCommand())
                    {
                        checkForTableCommand.CommandText = "select count(*) from sys.tables where name = 'Events'";
                        exists = (int)checkForTableCommand.ExecuteScalar();
                    }
                    if(exists == 0)
                    {
                        using(var createTableCommand = _connection.CreateCommand())
                        {
                            createTableCommand.CommandText =
                                @"
CREATE TABLE [dbo].[Events](
	[AggregateId] [uniqueidentifier] NOT NULL,
	[AggregateVersion] [int] NOT NULL,
	[TimeStamp] [datetime] NOT NULL,
	[EventType] [varchar](300) NOT NULL,
    [EventId] [uniqueidentifier] NOT NULL,
	[Event] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_Events] PRIMARY KEY CLUSTERED 
(
	[AggregateId] ASC,
	[AggregateVersion] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[Events] ADD  CONSTRAINT [DF_Events_TimeStamp]  DEFAULT (getdate()) FOR [TimeStamp]

";
                            createTableCommand.ExecuteNonQuery();
                        }
                        EventsTableVerifiedToExist = true;
                    }
                }
            }
        }

        protected override IEnumerable<IAggregateRootEvent> GetHistoryUnSafe(Guid aggregateId)
        {
            using (var _connection = OpenSession())
            {
                using(var loadCommand = _connection.CreateCommand())
                {
                    loadCommand.CommandText =
                        "SELECT EventType, Event, AggregateVersion, EventId FROM Events WHERE AggregateId = @AggregateId ORDER BY AggregateVersion ASC";
                    loadCommand.Parameters.Add(new SqlParameter("AggregateId", aggregateId));

                    using(var eventReader = loadCommand.ExecuteReader())
                    {
                        for(var version = 1; eventReader.Read(); version++)
                        {
                            var @event = DeserializeEvent(eventReader.GetString(0), eventReader.GetString(1));
                            @event.AggregateRootVersion = eventReader.GetInt32(2);
                            @event.EventId = eventReader.GetGuid(3);
                            @event.AggregateRootId = aggregateId;

                            yield return @event;
                        }
                    }
                }
            }
        }

        private IAggregateRootEvent DeserializeEvent(string eventType, string eventData)
        {
            return (IAggregateRootEvent)JsonConvert.DeserializeObject(eventData, eventType.AsType(), JsonSettings);
        }

        protected override void SaveEvents(IEnumerable<IAggregateRootEvent> events)
        {
            using (var _connection = OpenSession())
            {
                var eventCount = events.Count();
                var handled = 0;
                while(handled < eventCount)
                {
                    //Console.WriteLine("Starting new sql batch");
                    using(var command = _connection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;
                        for(var handledInBatch = 0; handledInBatch < SqlBatchSize && handled < eventCount; handledInBatch++, handled++)
                        {
                            var @event = events.ElementAt(handledInBatch);

                            command.CommandText += "INSERT Events(AggregateId, AggregateVersion, EventType, EventId, Event) VALUES(@AggregateId{0}, @AggregateVersion{0}, @EventType{0}, @EventId{0}, @Event{0})"
                                .FormatWith(handledInBatch);

                            command.Parameters.Add(new SqlParameter("AggregateId" + handledInBatch, @event.AggregateRootId));
                            command.Parameters.Add(new SqlParameter("AggregateVersion" + handledInBatch, @event.AggregateRootVersion));
                            command.Parameters.Add(new SqlParameter("EventType" + handledInBatch, @event.GetType().FullName));
                            command.Parameters.Add(new SqlParameter("EventId" + handledInBatch, @event.EventId));
                            command.Parameters.Add(new SqlParameter("Event" + handledInBatch,
                                                                    JsonConvert.SerializeObject(@event, Formatting.Indented, JsonSettings)));
                        }
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        private bool _disposed;

        public override void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                //Console.WriteLine("{0}: {1}", GetType().Name, --instances);
                _idMap.Clear();
            }
        }

        public void ResetDB()
        {
            using (var _connection = OpenSession())
            {
                using(var dropCommand = _connection.CreateCommand())
                {
                    dropCommand.CommandText =
                        @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Events]') AND type in (N'U'))
DROP TABLE [dbo].[Events]";

                    dropCommand.ExecuteNonQuery();
                    EventsTableVerifiedToExist = false;
                }
            }
            EnsureEventsTableExists();
        }
    }
}