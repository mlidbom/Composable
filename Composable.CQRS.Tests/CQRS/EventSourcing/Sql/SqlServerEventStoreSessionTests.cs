using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.Persistence.EventStore;
using Composable.System.Linq;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.Sql
{
    [TestFixture]
    class SqlServerEventStoreSessionTests : EventStoreSessionTests
    {
        protected override IServiceLocator CreateServiceLocator() => TestWiringHelper.SetupTestingServiceLocator(TestingMode.RealComponents);

        [Test, LongRunning]
        public void Serializes_access_to_an_aggregate_so_that_concurrent_transactions_succeed_even_if_history_has_been_read_outside_of_modifying_transactions()
        {
            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());
            UseInTransactionalScope(session =>
                                    {
                                        session.Save(user);
                                        user.ChangeEmail("newemail@somewhere.not");
                                    });

            void UpdateEmail()
            {
                UseInScope(session =>
                                        {
                                            ((IEventStoreReader)session).GetHistory(user.Id);
                                            ServiceLocator.ExecuteUnitOfWork(() =>
                                                                             {
                                                                                 var userToUpdate = session.Get<User>(user.Id);
                                                                                 userToUpdate.ChangeEmail($"newemail_{userToUpdate.Version}@somewhere.not");
                                                                                 Thread.Sleep(100);
                                                                             });
                                        }); //Sql duplicate key (AggregateId, Version) Exception would be thrown here if history was not serialized
            }

            var tasks = 1.Through(20).Select(_ => Task.Factory.StartNew(UpdateEmail)).ToArray();
            Task.WaitAll(tasks);

            UseInScope(
                session =>
                {
                    var userHistory = ((IEventStoreReader)session).GetHistory(user.Id)
                                                                  .ToArray(); //Reading the aggregate will throw an exception if the history is invalid.
                    userHistory.Length.Should()
                               .Be(22); //Make sure that all of the transactions completed
                });
        }

        [Test, LongRunning]
        public void Serializes_access_to_an_aggregate_so_that_concurrent_transactions_succeed()
        {
            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());
            UseInTransactionalScope(session =>
                                    {
                                        session.Save(user);
                                        user.ChangeEmail("newemail@somewhere.not");
                                    });

            void UpdateEmail()
            {
                UseInTransactionalScope(session =>
                                        {
                                            var userToUpdate = session.Get<User>(user.Id);
                                            userToUpdate.ChangeEmail($"newemail_{userToUpdate.Version}@somewhere.not");
                                            Thread.Sleep(100);
                                        }); //Sql duplicate key (AggregateId, Version) Exception would be thrown here if history was not serialized
            }

            var tasks = 1.Through(20).Select(_ => Task.Factory.StartNew(UpdateEmail)).ToArray();


            Task.WaitAll(tasks);

            UseInScope(
                session =>
                {
                    var userHistory = ((IEventStoreReader)session).GetHistory(user.Id)
                                                                  .ToArray(); //Reading the aggregate will throw an exception if the history is invalid.
                    userHistory.Length.Should()
                               .Be(22); //Make sure that all of the transactions completed
                });
        }
    }
}