#region usings

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using Composable.NewtonSoft;
using Composable.System;
using Newtonsoft.Json;
using Composable.System.Linq;

#endregion

namespace Composable.CQRS.EventSourcing.SQLServer
{
    public class SqlServerEventStore : IEventStore
    {
        private readonly string _connectionString;

        public SqlServerEventStore(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEventStoreSession OpenSession()
        {
            return new EventStoreSessionDisposeWrapper(new SQLServerEventStoreSession(this));
        }

        private class SQLServerEventStoreSession : EventStoreSession
        {
            private static bool EventsTableVerifiedToExist;
            private readonly SqlServerEventStore _store;
            private readonly SqlConnection _connection;
            private int SqlBatchSize = 10;

            private readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
                                                                       {
                                                                           ContractResolver = new IncludeMembersWithPrivateSettersResolver()
                                                                       };

            public SQLServerEventStoreSession(SqlServerEventStore store)
            {
                _store = store;
                _connection = new SqlConnection(_store._connectionString);
                _connection.Open();
                EnsureEventsTableExists();
            }

            private void EnsureEventsTableExists()
            {
                if(!EventsTableVerifiedToExist)
                {
                    var checkForTableCommand = _connection.CreateCommand();
                    checkForTableCommand.CommandText = "select count(*) from sys.tables where name = 'Events'";
                    var exists = (int)checkForTableCommand.ExecuteScalar();
                    if(exists == 0)
                    {
                        var createTableCommand = _connection.CreateCommand();
                        createTableCommand.CommandText =
                            @"
CREATE TABLE [dbo].[Events](
	[AggregateId] [uniqueidentifier] NOT NULL,
	[AggregateVersion] [int] NOT NULL,
	[TimeStamp] [datetime] NOT NULL,
	[EventType] [varchar](300) NOT NULL,
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

            protected override IEnumerable<IAggregateRootEvent> GetHistoryUnSafe(Guid aggregateId)
            {
                var loadCommand = _connection.CreateCommand();
                loadCommand.CommandText = "SELECT EventType, Event FROM Events WHERE AggregateId = @AggregateId ORDER BY AggregateVersion ASC";
                loadCommand.Parameters.Add(new SqlParameter("AggregateId", aggregateId));

                using(var eventReader = loadCommand.ExecuteReader())
                {
                    for(var version = 1; eventReader.Read(); version++)
                    {
                        var @event = DeserializeEvent(eventReader.GetString(0), eventReader.GetString(1));
                        @event.AggregateRootVersion = version;
                        @event.AggregateRootId = aggregateId;
                        yield return @event;
                    }
                }
            }

            private IAggregateRootEvent DeserializeEvent(string eventType, string eventData)
            {
                return (IAggregateRootEvent)JsonConvert.DeserializeObject(eventData, FindType(eventType), JsonSettings);
            }


            private static readonly Dictionary<string,Type> _typeMap = new Dictionary<string, Type>();
            private static Type FindType(string valueType)
            {
                Type type;
                if(_typeMap.TryGetValue(valueType, out type))
                {
                    return type;
                }

                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(assembly => assembly.GetType(valueType))
                    .Where(t => t != null)
                    .ToArray();
                if(types.None())
                {
                    throw new FailedToFindTypeException(valueType);
                }

                if(types.Count() > 1)
                {
                    throw new MultipleMatchingTypesException(valueType);
                }

                type = types.Single();
                _typeMap.Add(valueType, types.Single());
                return type;
            }

            protected override void SaveEvents(IEnumerable<IAggregateRootEvent> events)
            {
                var eventCount = events.Count();
                var handled = 0;
                while(handled < eventCount)
                {
                    //Console.WriteLine("Starting new sql batch");
                    var command = _connection.CreateCommand();
                    command.CommandType = CommandType.Text;
                    for(var handledInBatch = 0; handledInBatch < SqlBatchSize && handled < eventCount; handledInBatch++, handled++)
                    {
                        var @event = events.ElementAt(handledInBatch);

                        command.CommandText += "INSERT Events(AggregateId, AggregateVersion, EventType, Event) VALUES(@AggregateId{0}, @AggregateVersion{0}, @EventType{0}, @Event{0})"
                            .FormatWith(handledInBatch);

                        command.Parameters.Add(new SqlParameter("AggregateId" + handledInBatch, @event.AggregateRootId));
                        command.Parameters.Add(new SqlParameter("AggregateVersion" + handledInBatch, @event.AggregateRootVersion));
                        command.Parameters.Add(new SqlParameter("EventType" + handledInBatch, @event.GetType().FullName));
                        command.Parameters.Add(new SqlParameter("Event" + handledInBatch, JsonConvert.SerializeObject(@event, Formatting.None, JsonSettings)));
                    }
                    command.ExecuteNonQuery();
                }
            }

            public override void Dispose()
            {
                _connection.Dispose();
                _idMap.Clear();
            }

            public void PurgeDB()
            {
                var dropCommand = _connection.CreateCommand();
                dropCommand.CommandText =
                    @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Events]') AND type in (N'U'))
DROP TABLE [dbo].[Events]";

                dropCommand.ExecuteNonQuery();
                EventsTableVerifiedToExist = false;
            }
        }

        public static void ResetDB(string connectionString)
        {
            var me = new SqlServerEventStore(connectionString);
            using(var session = new SQLServerEventStoreSession(me))
            {
                session.PurgeDB();
            }
        }
    }

    internal class MultipleMatchingTypesException : Exception
    {
        public MultipleMatchingTypesException(string typeName):base(typeName)
        {            
        }
    }

    internal class FailedToFindTypeException : Exception
    {
        public FailedToFindTypeException(string typeName):base(typeName)
        {            
        }
    }
}