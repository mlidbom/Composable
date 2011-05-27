#region usings

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Composable.NewtonSoft;
using Composable.System;
using Newtonsoft.Json;

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
	[Discriminator] [varchar](300) NOT NULL,
	[Event] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_Events] PRIMARY KEY CLUSTERED 
(
	[AggregateId] ASC, [AggregateVersion] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
";
                        createTableCommand.ExecuteNonQuery();
                    }
                    EventsTableVerifiedToExist = true;
                }
            }

            protected override IEnumerable<IAggregateRootEvent> GetHistoryUnSafe(Guid aggregateId)
            {
                var loadCommand = _connection.CreateCommand();
                loadCommand.CommandText = "SELECT Discriminator, Event FROM Events WHERE AggregateId = @AggregateId ORDER BY AggregateVersion ASC";
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

            private IAggregateRootEvent DeserializeEvent(string discriminator, string eventData)
            {
                return (IAggregateRootEvent)JsonConvert.DeserializeObject(eventData, Type.GetType(discriminator), JsonSettings);
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

                        command.CommandText += "INSERT Events(AggregateId, AggregateVersion, Discriminator, Event) VALUES(@AggregateId{0}, @AggregateVersion{0}, @Discriminator{0}, @Event{0})"
                            .FormatWith(handledInBatch);

                        command.Parameters.Add(new SqlParameter("AggregateId" + handledInBatch, @event.AggregateRootId));
                        command.Parameters.Add(new SqlParameter("AggregateVersion" + handledInBatch, @event.AggregateRootVersion));
                        command.Parameters.Add(new SqlParameter("Discriminator" + handledInBatch, @event.GetType().AssemblyQualifiedName));
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
}