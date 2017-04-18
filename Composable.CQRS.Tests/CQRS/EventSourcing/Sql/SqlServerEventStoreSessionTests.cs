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
        protected override IServiceLocator CreateServiceLocator() => TestWiringHelper.SetupTestingServiceLocator(TestingMode.DatabasePool);

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

            void UpdateEmail(WaitHandle waitToCompleteTransaction, ManualResetEvent readyToStart, ManualResetEvent signalReadyToCompleteTransaction)
            {
                UseInScope(session =>
                                        {
                                            ((IEventStoreReader)session).GetHistory(user.Id);
                                            ServiceLocator.ExecuteUnitOfWork(() =>
                                                                             {
                                                                                 readyToStart.Set();
                                                                                 var userToUpdate = session.Get<User>(user.Id);
                                                                                 userToUpdate.ChangeEmail($"newemail_{userToUpdate.Version}@somewhere.not");
                                                                                 signalReadyToCompleteTransaction.Set();
                                                                                 waitToCompleteTransaction.WaitOne();
                                                                             });
                                        });
            }

            var readyToComplete = new ManualResetEvent(false);
            var threads = 2;
            var resetEvents = 1.Through(threads)
                               .Select(_ => new
                                            {
                                                ReadyToStart = new ManualResetEvent(false),
                                                AllowedToComplete = new ManualResetEvent(false)
                                            })
                               .ToList();
            var tasks = resetEvents.Select(resetEvent => Task.Factory.StartNew(() => UpdateEmail(resetEvent.AllowedToComplete, resetEvent.ReadyToStart, readyToComplete))).ToArray();

            resetEvents.ForEach(@this => @this.ReadyToStart.WaitOne());
            readyToComplete.WaitOne();
            Thread.Sleep(50);
            resetEvents.ForEach(@this => @this.AllowedToComplete.Set());

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

            void UpdateEmail(WaitHandle waitToCompleteTransaction, ManualResetEvent readyToStart, ManualResetEvent signalReadyToCompleteTransaction)
            {
                UseInTransactionalScope(session =>
                                        {
                                            readyToStart.Set();
                                            var userToUpdate = session.Get<User>(user.Id);
                                            userToUpdate.ChangeEmail($"newemail_{userToUpdate.Version}@somewhere.not");
                                            signalReadyToCompleteTransaction.Set();
                                            waitToCompleteTransaction.WaitOne();
                                        });
            }

            var readyToComplete = new ManualResetEvent(false);
            var threads = 2;
            var resetEvents = 1.Through(threads)
                               .Select(_ => new
                                            {
                                                ReadyToStart = new ManualResetEvent(false),
                                                AllowedToComplete = new ManualResetEvent(false)
                                            })
                               .ToList();
            var tasks = resetEvents.Select(resetEvent => Task.Factory.StartNew(() => UpdateEmail(resetEvent.AllowedToComplete, resetEvent.ReadyToStart, readyToComplete))).ToArray();

            resetEvents.ForEach(@this => @this.ReadyToStart.WaitOne());
            readyToComplete.WaitOne();
            Thread.Sleep(50);
            resetEvents.ForEach(@this => @this.AllowedToComplete.Set());


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
    }
}