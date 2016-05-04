using System;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.MicrosoftSQLServer;
using Composable.CQRS.Testing;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using NCrunch.Framework;
using NUnit.Framework;

namespace CQRS.Tests.CQRS.EventSourcing.Sql
{
    [TestFixture]
    [ExclusivelyUses(NCrunchExlusivelyUsesResources.EventStoreDbMdf)]
    class SqlServerEventStoreSessionTests : EventStoreSessionTests
    {
        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString;
        [TestFixtureSetUp]
        public static void SetupFixture()
        {
            SqlServerEventStore.ResetDB(ConnectionString);
        }

        protected override IEventStore CreateStore()
        {
            return new SqlServerEventStore(ConnectionString, new SingleThreadUseGuard());
        }

        [Test]
        public void Serializes_access_to_an_aggregate_so_that_concurrent_transactions_succeed_even_if_history_has_been_read_outside_of_modifying_transactions()
        {
            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());
            using (var session = OpenSession(CreateStore()))
            {
                session.Save(user);
                user.ChangeEmail($"newemail@somewhere.not");
                session.SaveChanges();
            }

            Action updateEmail = () =>
            {
                using (var session = OpenSession(CreateStore()))
                {
                    var prereadHistory = ((IEventStoreReader)session).GetHistory(user.Id);
                    using (var transaction = new TransactionScope())
                    {
                        var userToUpdate = session.Get<User>(user.Id);
                        userToUpdate.ChangeEmail($"newemail_{userToUpdate.Version}@somewhere.not");
                        Thread.Sleep(100);
                        session.SaveChanges();
                        transaction.Complete();
                    }//Sql duplicate key (AggregateId, Version) Exception would be thrown here if history was not serialized 
                }
            };

            var tasks = 1.Through(20).Select(_ => Task.Factory.StartNew(updateEmail)).ToArray();
            Task.WaitAll(tasks);

            using (var session = OpenSession(CreateStore()))
            {
                var userHistory = ((IEventStoreReader)session).GetHistory(user.Id).ToArray();//Reading the aggregate will throw an exception if the history is invalid.
            }
        }

        [Test]
        public void Serializes_access_to_an_aggregate_so_that_concurrent_transactions_succeed()
        {
            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());
            using (var session = OpenSession(CreateStore()))
            {
                session.Save(user);
                user.ChangeEmail($"newemail@somewhere.not");
                session.SaveChanges();
            }

            Action updateEmail = () =>
            {
                using (var session = OpenSession(CreateStore()))
                {
                    using (var transaction = new TransactionScope())
                    {
                        var userToUpdate = session.Get<User>(user.Id);
                        userToUpdate.ChangeEmail($"newemail_{userToUpdate.Version}@somewhere.not");
                        Thread.Sleep(100);
                        session.SaveChanges();
                        transaction.Complete();
                    }//Sql duplicate key (AggregateId, Version) Exception would be thrown here if history was not serialized 
                }
            };

            var tasks = 1.Through(20).Select(_ => Task.Factory.StartNew(updateEmail)).ToArray();
            

            Task.WaitAll(tasks);

            using (var session = OpenSession(CreateStore()))
            {
                var userHistory = ((IEventStoreReader)session).GetHistory(user.Id).ToArray();//Reading the aggregate will throw an exception if the history is invalid.
            }
        }

        [Test]
        public void InsertNewEventType_should_not_throw_exception_if_the_event_type_has_been_inserted_by_something_else()
        {
            Action<Guid> changeUserEmailInOtherAppDomain = userId =>
                                                           {
                                                               using(AppDomain.CurrentDomain.CloneScope())
                                                               {
                                                                   using(var session = OpenSession(CreateStore()))
                                                                   {
                                                                       var user = session.Get<User>(userId);
                                                                       user.ChangeEmail("some@email.new");
                                                                       session.SaveChanges();
                                                                   }
                                                               }
                                                           };

            using (var session = OpenSession(CreateStore()))
            {
                var userId = Guid.NewGuid();
                var user = new User();
                user.Register("email@email.se", "password", userId);
                session.Save(user);
                session.SaveChanges();

                changeUserEmailInOtherAppDomain(userId);

                user.ChangeEmail("some@email.new");
                session.SaveChanges();
            }
        }
    }
}