﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.Persistence.EventStore;
using Composable.System.Linq;
using Composable.System.Transactions;
using Composable.SystemExtensions.Threading;
using Composable.Testing;
using Composable.Testing.Threading;
using Composable.Tests.CQRS;
using FluentAssertions;
using NUnit.Framework;
using Composable.System;

namespace Composable.Tests.ExternalDependencies.CQRS.EventSourcing.Sql
{
    //urgent: Merge into parent class since we only have one now.
    [TestFixture]
    class SqlServerEventStoreUpdaterTests : EventStoreUpdaterTests
    {
        protected override IServiceLocator CreateServiceLocator() => TestWiringHelper.SetupTestingServiceLocator();

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

            var getHistorySection = GatedCodeSection.WithTimeout(2.Seconds());
            var changeEmailSection = GatedCodeSection.WithTimeout(2.Seconds());

            void UpdateEmail()
            {
                UseInScope(session =>
                                        {
                                            using(getHistorySection.Enter())
                                            {
                                                ((IEventStoreReader)session).GetHistory(user.Id);
                                            }
                                            TransactionScopeCe.Execute(() =>
                                            {
                                                using(changeEmailSection.Enter())
                                                {
                                                    var userToUpdate = session.Get<User>(user.Id);
                                                    userToUpdate.ChangeEmail($"newemail_{userToUpdate.Version}@somewhere.not");
                                                }
                                            });
                                        });
            }

            var threads = 2;
            var tasks = 1.Through(threads).Select(resetEvent => Task.Factory.StartNew(UpdateEmail)).ToArray();

            getHistorySection.LetOneThreadPass();
            changeEmailSection.LetOneThreadEnterAndReachExit();
            changeEmailSection.Open();
            getHistorySection.Open();

            Task.WaitAll(tasks);//Sql duplicate key (AggregateId, Version) Exception would be thrown here if history was not serialized

            UseInScope(
                session =>
                {
                    var userHistory = ((IEventStoreReader)session).GetHistory(user.Id)
                                                                  .ToArray(); //Reading the aggregate will throw an exception if the history is invalid.
                    userHistory.Length.Should()
                               .Be(threads + 2); //Make sure that all of the transactions completed
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


            var changeEmailSection = GatedCodeSection.WithTimeout(2.Seconds());
            void UpdateEmail()
            {
                UseInTransactionalScope(session =>
                                        {
                                            using(changeEmailSection.Enter())
                                            {
                                                var userToUpdate = session.Get<User>(user.Id);
                                                userToUpdate.ChangeEmail($"newemail_{userToUpdate.Version}@somewhere.not");
                                            }
                                        });
            }

               var threads = 2;

            var tasks = 1.Through(threads).Select(resetEvent => Task.Factory.StartNew(UpdateEmail)).ToArray();

            changeEmailSection.EntranceGate.Open();
            changeEmailSection.EntranceGate.AwaitPassedThroughCountEqualTo(2);
            changeEmailSection.ExitGate.AwaitQueueLengthEqualTo(1);

            Thread.Sleep(10.Milliseconds());

            changeEmailSection.ExitGate.Queued.Should().Be(1, "One thread should be blocked by transaction and never reach here until the other completes the transaction.");

            changeEmailSection.Open();

            Task.WaitAll(tasks);//Sql duplicate key (AggregateId, Version) Exception would be thrown here if history was not serialized. Or a deadlock will be thrown if the locking is not done correctly.

            UseInScope(
                session =>
                {
                    var userHistory = ((IEventStoreReader)session).GetHistory(user.Id)
                                                                  .ToArray(); //Reading the aggregate will throw an exception if the history is invalid.
                    userHistory.Length.Should()
                               .Be(threads + 2); //Make sure that all of the transactions completed
                });
        }

        [Test]
        public void If_an_updater_is_used_in_two_transactions_an_exception_is_thrown()
        {
            using (ServiceLocator.BeginScope())
            {
                using var updater = ServiceLocator.Resolve<ITestingEventStoreUpdater>();
                var user = new User();
                user.Register("email@email.se", "password", Guid.NewGuid());

                TransactionScopeCe.Execute(() => updater.Save(user));
                AssertThrows.Exception<ComponentUsedByMultipleTransactionsException>(() => TransactionScopeCe.Execute(() => updater.Get<User>(user.Id)));
            }
        }
    }
}