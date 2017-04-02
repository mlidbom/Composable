using System;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.MicrosoftSQLServer;
using Composable.Persistence.EventStore.Serialization.NewtonSoft;
using Composable.System.Linq;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.Sql
{
    [TestFixture]
    class SqlServerEventStoreSessionTests : EventStoreSessionTests
    {
        string _connectionString;
        SqlServerDatabasePool _databasePool;
        IServiceLocator _serviceLocator;
        [SetUp]
        public void Setup()
        {
            _serviceLocator = DependencyInjectionContainer.CreateServiceLocatorForTesting(
                                                                                          container => container
                                                                                              .RegisterSqlServerEventStore<ITestingEventstoreSession, ITestingEventstoreReader
                                                                                              >("SqlServerEventStoreSessionTests_EventStore"));

            var masterConnectionString = ConfigurationManager.ConnectionStrings["MasterDb"].ConnectionString;
            _databasePool = new SqlServerDatabasePool(masterConnectionString);
            _connectionString = _databasePool.ConnectionStringFor("SqlServerEventStoreSessionTests_EventStore");
        }

        [TearDown]
        public void TearDownTask() {
            _databasePool.Dispose();
            _serviceLocator.Dispose();
        }

        protected override IEventStore CreateStore() => new EventStore(_connectionString, serializer: new NewtonSoftEventStoreEventSerializer());

        [Test]
        public void Serializes_access_to_an_aggregate_so_that_concurrent_transactions_succeed_even_if_history_has_been_read_outside_of_modifying_transactions()
        {
            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());
            using (var session = OpenSession(CreateStore()))
            {
                session.Save(user);
                user.ChangeEmail("newemail@somewhere.not");
                session.SaveChanges();
            }

            void UpdateEmail()
            {
                using(var session = OpenSession(CreateStore()))
                {
                    ((IEventStoreReader)session).GetHistory(user.Id);
                    using(var transaction = new TransactionScope())
                    {
                        var userToUpdate = session.Get<User>(user.Id);
                        userToUpdate.ChangeEmail($"newemail_{userToUpdate.Version}@somewhere.not");
                        Thread.Sleep(100);
                        session.SaveChanges();
                        transaction.Complete();
                    } //Sql duplicate key (AggregateId, Version) Exception would be thrown here if history was not serialized
                }
            }

            var tasks = 1.Through(20).Select(_ => Task.Factory.StartNew(UpdateEmail)).ToArray();
            Task.WaitAll(tasks);

            using (var session = OpenSession(CreateStore()))
            {
                var userHistory = ((IEventStoreReader)session).GetHistory(user.Id).ToArray();//Reading the aggregate will throw an exception if the history is invalid.
                userHistory.Length.Should().Be(22);//Make sure that all of the transactions completed
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
                user.ChangeEmail("newemail@somewhere.not");
                session.SaveChanges();
            }

            void UpdateEmail()
            {
                using(var session = OpenSession(CreateStore()))
                {
                    using(var transaction = new TransactionScope())
                    {
                        var userToUpdate = session.Get<User>(user.Id);
                        userToUpdate.ChangeEmail($"newemail_{userToUpdate.Version}@somewhere.not");
                        Thread.Sleep(100);
                        session.SaveChanges();
                        transaction.Complete();
                    } //Sql duplicate key (AggregateId, Version) Exception would be thrown here if history was not serialized
                }
            }

            var tasks = 1.Through(20).Select(_ => Task.Factory.StartNew(UpdateEmail)).ToArray();


            Task.WaitAll(tasks);

            using (var session = OpenSession(CreateStore()))
            {
                ((IEventStoreReader)session).GetHistory(user.Id);
            }
        }

        [Test]
        public void InsertNewEventType_should_not_throw_exception_if_the_event_type_has_been_inserted_by_something_else()
        {
            void ChangeAnotherUsersEmailInOtherAppDomain() => AppDomainExtensions.ExecuteInCloneDomainScope(() =>
                                                                                                            {
                                                                                                                var test = new SqlServerEventStoreSessionTests();
                                                                                                                try
                                                                                                                {
                                                                                                                    test.Setup();
                                                                                                                    using(var session = test.OpenSession(test.CreateStore()))
                                                                                                                    {
                                                                                                                        var otherUser = User.Register(session,
                                                                                                                                                      "email@email.se",
                                                                                                                                                      "password",
                                                                                                                                                      Guid.NewGuid());
                                                                                                                        otherUser.ChangeEmail("some@email.new");
                                                                                                                        session.SaveChanges();
                                                                                                                    }
                                                                                                                }
                                                                                                                finally
                                                                                                                {
                                                                                                                    test.TearDownTask();
                                                                                                                }
                                                                                                            },
                                                                                                            disposeDelay: 100.Milliseconds());

            using (var session = OpenSession(CreateStore()))
            {
                var user = User.Register(session, "email@email.se", "password", Guid.NewGuid());
                session.SaveChanges();

                ChangeAnotherUsersEmailInOtherAppDomain();

                user.ChangeEmail("some@email.new");
                session.SaveChanges();
            }
        }
    }
}