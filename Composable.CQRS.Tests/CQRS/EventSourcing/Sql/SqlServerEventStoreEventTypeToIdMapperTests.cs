using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.MicrosoftSQLServer;
using Composable.SystemExtensions.Threading;
using NCrunch.Framework;
using NUnit.Framework;

namespace CQRS.Tests.CQRS.EventSourcing.Sql
{
    [TestFixture]
    [ExclusivelyUses(NCrunchExlusivelyUsesResources.EventStoreDbMdf)]
    public class SqlServerEventStoreEventTypeToIdMapperTests
    {
        [Test]
        public void InsertNewEventType_should_not_throw_exception_if_the_event_type_has_been_inserted_by_something_else()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString;

            var eventStore = new SqlServerEventStore(connectionString, new SingleThreadUseGuard());

            var user = new User();
            var stored = (IEventStored)user;

            user.Register("email@email.se", "password", Guid.NewGuid());
            eventStore.SaveEvents(stored.GetChanges());
            //The database only stores UserRegistered event type mapping.

            stored.AcceptChanges(); //Clear uncommittedEvents.

            user.ChangeEmail("the_new_email@email.se");

            Mock_the_UserChangedEmail_event_type_has_been_inserted_by_something_else(connectionString);

            //Try insert UserChangedEmail event type into database.
            eventStore.SaveEvents(stored.GetChanges());
        }

        private static void Mock_the_UserChangedEmail_event_type_has_been_inserted_by_something_else(string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"INSERT {EventTypeTable.Name} ( {EventTypeTable.Columns.EventType} ) VALUES( @{EventTypeTable.Columns.EventType} )";
                    command.Parameters.Add(new SqlParameter(EventTypeTable.Columns.EventType, typeof(UserChangedEmail).FullName));
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
