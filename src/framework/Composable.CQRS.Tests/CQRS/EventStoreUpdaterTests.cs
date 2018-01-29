using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Transactions;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Testing;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore;
using Composable.Refactoring.Naming;
using Composable.System;
using Composable.System.Diagnostics;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using Composable.Testing;
using Composable.Testing.Performance;
using Composable.Tests.System.TransactionsApiExploration;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Composable.Tests.CQRS
{
    [TestFixture]
    public abstract class EventStoreUpdaterTests
    {
        class EventSpy
        {
            public IEnumerable<BusApi.RemoteSupport.ExactlyOnce.IEvent> DispatchedMessages => _events.ToList();
            public void Receive(BusApi.RemoteSupport.ExactlyOnce.IEvent @event) { _events.Add(@event); }
            readonly List<BusApi.RemoteSupport.ExactlyOnce.IEvent> _events = new List<BusApi.RemoteSupport.ExactlyOnce.IEvent>();
        }

        EventSpy _eventSpy;

        protected abstract IServiceLocator CreateServiceLocator();
        internal IServiceLocator ServiceLocator { get; private set; }

        [SetUp] public void SetupBus()
        {
            ServiceLocator = CreateServiceLocator();

            _eventSpy = new EventSpy();

            ServiceLocator.Resolve<IMessageHandlerRegistrar>()
                          .ForEvent<BusApi.RemoteSupport.ExactlyOnce.IEvent>(_eventSpy.Receive);

            ServiceLocator.Resolve<ITypeMappingRegistar>()
                          .Map<Composable.Tests.CQRS.User>("2cfabb11-5e5a-494d-898f-8bfc654544eb")
                          .Map<Composable.Tests.CQRS.IUserEvent>("0727c209-2f49-46ab-a56b-a1332415a895")
                          .Map<Composable.Tests.CQRS.MigratedAfterUserChangedEmailEvent>("9ff42a12-f28c-447a-8aa1-79e6f685fa41")
                          .Map<Composable.Tests.CQRS.MigratedBeforeUserRegisteredEvent>("3338e1d4-3839-4f63-9248-ea4dd30c8348")
                          .Map<Composable.Tests.CQRS.MigratedReplaceUserChangedPasswordEvent>("45db6370-f7e7-4eb8-b792-845485d86295")
                          .Map<Composable.Tests.CQRS.UserChangedEmail>("40ae1f6d-5f95-4c60-ac5f-21a3d1c85de9")
                          .Map<Composable.Tests.CQRS.UserChangedPassword>("0b3b57f6-fd69-4da1-bb52-15033495f044")
                          .Map<Composable.Tests.CQRS.UserEvent>("fa71e035-571d-4231-bd65-e667c138ec36")
                          .Map<Composable.Tests.CQRS.UserRegistered>("03265864-8e1d-4eb7-a7a9-63dfc2b965de");
        }

        [TearDown] public void TearDownTask()
        {
            ServiceLocator.Dispose();
        }


        protected void UseInTransactionalScope([InstantHandle] Action<IEventStoreUpdater> useSession)
            => ServiceLocator.ExecuteTransactionInIsolatedScope(
                () => useSession(ServiceLocator.Resolve<ITestingEventStoreUpdater>()));

        protected void UseInScope([InstantHandle]Action<IEventStoreUpdater> useSession)
            => ServiceLocator.ExecuteInIsolatedScope(
                () => useSession(ServiceLocator.Resolve<ITestingEventStoreUpdater>()));

        [Test]
        public void WhenFetchingAggregateThatDoesNotExistNoSuchAggregateExceptionIsThrown()
        {
            UseInScope(session => Assert.Throws<AggregateNotFoundException>(() => session.Get<User>(Guid.NewGuid())));
        }

        [Test]
        public void CanSaveAndLoadAggregate()
        {
            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());
            user.ChangePassword("NewPassword");
            user.ChangeEmail("NewEmail");

            UseInTransactionalScope(session => session.Save(user));

            UseInScope(session =>
                       {
                           var loadedUser = session.Get<User>(user.Id);

                           Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
                           Assert.That(loadedUser.Email, Is.EqualTo(user.Email));
                           Assert.That(loadedUser.Password, Is.EqualTo(user.Password));

                       });
        }

        [Test]
        public void ThrowsIfUsedByMultipleThreads()
        {
            IEventStoreUpdater updater = null;
            IEventStoreReader reader = null;
            var wait = new ManualResetEventSlim();
            ThreadPool.QueueUserWorkItem(_ =>
                                         {
                                             ServiceLocator.ExecuteInIsolatedScope(() =>
                                                                                   {
                                                                                       updater = ServiceLocator.Resolve<ITestingEventStoreUpdater>();
                                                                                       reader = ServiceLocator.Resolve<ITestingEventStoreReader>();
                                                                                   });
                                             wait.Set();
                                         });
            wait.Wait();

            Assert.Throws<MultiThreadedUseException>(() => updater.Get<User>(Guid.NewGuid()));
            Assert.Throws<MultiThreadedUseException>(() => updater.Dispose());
            Assert.Throws<MultiThreadedUseException>(() => reader.GetReadonlyCopyOfVersion<User>(Guid.NewGuid(), 1));
            Assert.Throws<MultiThreadedUseException>(() => updater.Save(new User()));
            Assert.Throws<MultiThreadedUseException>(() => updater.TryGet(Guid.NewGuid(), out User _));

        }

        [Test]
        public void CanLoadSpecificVersionOfAggregate()
        {
            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());
            user.ChangePassword("NewPassword");
            user.ChangeEmail("NewEmail");

            UseInTransactionalScope(session => session.Save(user));

            UseInScope(session =>
                       {
                           var reader = ServiceLocator.Resolve<ITestingEventStoreReader>();
                           var loadedUser = reader.GetReadonlyCopyOfVersion<User>(user.Id, 1);
                           Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
                           Assert.That(loadedUser.Email, Is.EqualTo("email@email.se"));
                           Assert.That(loadedUser.Password, Is.EqualTo("password"));

                           loadedUser = reader.GetReadonlyCopyOfVersion<User>(user.Id, 2);
                           Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
                           Assert.That(loadedUser.Email, Is.EqualTo("email@email.se"));
                           Assert.That(loadedUser.Password, Is.EqualTo("NewPassword"));

                           loadedUser = reader.GetReadonlyCopyOfVersion<User>(user.Id, 3);
                           Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
                           Assert.That(loadedUser.Email, Is.EqualTo("NewEmail"));
                           Assert.That(loadedUser.Password, Is.EqualTo("NewPassword"));
                       });
        }

        [Test]
        public void ReturnsSameInstanceOnRepeatedLoads()
        {
            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());

            UseInTransactionalScope(session => session.Save(user));

            UseInScope(session =>
                       {
                           var loaded1 = session.Get<User>(user.Id);
                           var loaded2 = session.Get<User>(user.Id);
                           Assert.That(loaded1, Is.SameAs(loaded2));
                       });
        }

        [Test]
        public void ReturnsSameInstanceOnLoadAfterSave()
        {
            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());

            UseInTransactionalScope(session =>
                                    {
                                        session.Save(user);

                                        var loaded1 = session.Get<User>(user.Id);
                                        var loaded2 = session.Get<User>(user.Id);
                                        Assert.That(loaded1, Is.SameAs(loaded2));
                                        Assert.That(loaded1, Is.SameAs(user));
                                    });
        }

        [Test]
        public void TracksAndUpdatesLoadedAggregates()
        {
            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());

            UseInTransactionalScope(session => session.Save(user));

            UseInTransactionalScope(session =>
                                    {
                                        var loadedUser = session.Get<User>(user.Id);
                                        loadedUser.ChangePassword("NewPassword");
                                    });

            UseInScope(session =>
                       {
                           var loadedUser = session.Get<User>(user.Id);
                           Assert.That(loadedUser.Password, Is.EqualTo("NewPassword"));
                       });
        }

        [Test]
        public void DoesNotUpdateAggregatesLoadedViaSpecificVersion()
        {
            var user = new User();
            user.Register("OriginalEmail", "password", Guid.NewGuid());

            UseInTransactionalScope(session => session.Save(user));

            UseInTransactionalScope(session =>
                                    {
                                        var loadedUser = ServiceLocator.Resolve<ITestingEventStoreReader>().GetReadonlyCopyOfVersion<User>(user.Id, 1);
                                        loadedUser.ChangeEmail("NewEmail");
                                    });

            UseInScope(session =>
                       {
                           var loadedUser = session.Get<User>(user.Id);
                           Assert.That(loadedUser.Email, Is.EqualTo("OriginalEmail"));
                       });
        }

        [Test]
        public void ResetsAggregatesAfterSaveChanges()
        {
            var user = new User();
            user.Register("OriginalEmail", "password", Guid.NewGuid());

            UseInTransactionalScope(session => session.Save(user));
            Assert.That(((IEventStored)user).GetChanges(), Is.Empty);
        }

        [Test, Ignore("TODO. Fix this long standing design issue. This test will probably be removed because get changes is refactored out of existence as we force the store updater to create the instances and it will immediately track updates via observables")]
        public void Resets_aggregate_immediately_upon_save()
        {
            var user = new User();
            user.Register("OriginalEmail", "password", Guid.NewGuid());

            UseInTransactionalScope(session =>
                                    {
                                        session.Save(user);
                                        Assert.That((user as IEventStored).GetChanges(), Is.Empty);
                                    });
        }

        [Test]
        public void ThrowsWhenAttemptingToSaveExistingAggregate()
        {
            var user = new User();
            user.Register("OriginalEmail", "password", Guid.NewGuid());

            UseInTransactionalScope(session => session.Save(user));

            UseInTransactionalScope(
                session => Assert.Throws<AttemptToSaveAlreadyPersistedAggregateException>(
                    () => session.Save(user)));
        }

        [Test]
        public void DoesNotExplodeWhenSavingMoreThan10Events()
        {
            var user = new User();
            user.Register("OriginalEmail", "password", Guid.NewGuid());
            1.Through(100).ForEach(index => user.ChangeEmail("email" + index));

            UseInTransactionalScope(session => session.Save(user));
        }

        [Test]
        public void AggregateCannotBeRetrievedAfterBeingDeleted()
        {
            var user1 = new User();
            user1.Register("email1@email.se", "password", Guid.NewGuid());

            var user2 = new User();
            user2.Register("email2@email.se", "password", Guid.NewGuid());

            UseInTransactionalScope(session =>
                                    {
                                        session.Save(user1);
                                        session.Save(user2);
                                    });

            UseInTransactionalScope(session =>
                                    {
                                        session.Delete(user1.Id);

                                        Assert.IsFalse(session.TryGet(user1.Id, out User _));

                                        var loadedUser2 = session.Get<User>(user2.Id);
                                        Assert.That(loadedUser2.Id, Is.EqualTo(user2.Id));
                                        Assert.That(loadedUser2.Email, Is.EqualTo(user2.Email));
                                        Assert.That(loadedUser2.Password, Is.EqualTo(user2.Password));
                                    });
        }

        [Test]
        public void DeletingAnAggregateDoesNotPreventEventsFromItFromBeingRaised()
        {
            var user1 = new User();
            user1.Register("email1@email.se", "password", Guid.NewGuid());

            var user2 = new User();
            user2.Register("email2@email.se", "password", Guid.NewGuid());

            UseInTransactionalScope(session =>
                                    {
                                        session.Save(user1);
                                        session.Save(user2);
                                    });

            _eventSpy.DispatchedMessages.Count().Should().Be(2);

            UseInTransactionalScope(session =>
                                    {
                                        user1 = session.Get<User>(user1.Id);

                                        user1.ChangeEmail("new_email");

                                        session.Delete(user1.Id);
                                    });

            var published = _eventSpy.DispatchedMessages.ToList();
            _eventSpy.DispatchedMessages.Count()
                      .Should()
                      .Be(3);
            Assert.That(published.Last(), Is.InstanceOf<UserChangedEmail>());
        }

        [Test,Ignore("TODO: Fix this long standing issue")] public void Events_should_be_published_immediately()
        {
            UseInTransactionalScope(session =>
                                    {
                                        var user1 = new User();
                                        user1.Register("email1@email.se", "password", Guid.NewGuid());

                                        _eventSpy.DispatchedMessages.Last()
                                                  .Should()
                                                  .BeOfType<UserRegistered>();

                                        user1 = session.Get<User>(user1.Id);
                                        user1.ChangeEmail("new_email");
                                        _eventSpy.DispatchedMessages.Last()
                                                  .Should()
                                                  .BeOfType<UserChangedEmail>();
                                    });
        }

        [Test]
        public void When_fetching_history_from_the_same_instance_after_updating_an_aggregate_the_fetched_history_includes_the_new_events()
        {
            var userId = Guid.NewGuid();
            UseInTransactionalScope(session =>
                                    {
                                        var user = new User();
                                        user.Register("test@email.com", "Password1", userId);
                                        session.Save(user);
                                    });

            UseInTransactionalScope(session =>
                                    {
                                        var user = session.Get<User>(userId);
                                        user.ChangeEmail("new_email@email.com");
                                    });

            UseInScope(session =>
                       {
                           var history = ((IEventStoreReader)session).GetHistory(userId);
                           Assert.That(history.Count, Is.EqualTo(2));
                       });
        }

        [Test]
        public void When_deleting_and_then_fetching_an_aggregates_history_the_history_should_be_gone()
        {
            var userId = Guid.NewGuid();

            UseInTransactionalScope(session =>
                                    {
                                        var user = new User();
                                        user.Register("test@email.com", "Password1", userId);
                                        session.Save(user);
                                    });

            UseInTransactionalScope(session => session.Delete(userId));

            UseInScope(session =>
                       {
                           var history = ((IEventStoreReader)session).GetHistory(userId);
                           Assert.That(history.Count, Is.EqualTo(0));
                       });
        }

        [Test]
        public void When_fetching_and_deleting_an_aggregate_then_fetching_history_again_the_history_should_be_gone()
        {
            var userId = Guid.NewGuid();

            UseInTransactionalScope(session =>
                                    {
                                        var user = new User();
                                        user.Register("test@email.com", "Password1", userId);
                                        session.Save(user);
                                    });

            UseInTransactionalScope(session =>
                                    {
                                        session.Get<User>(userId);
                                        session.Delete(userId);
                                    });

            UseInScope(session =>
                       {
                           var history = ((IEventStoreReader)session).GetHistory(userId);
                           Assert.That(history.Count, Is.EqualTo(0));
                       });
        }


        [Test]
        public void Concurrent_read_only_access_to_aggregate_history_can_occur_in_parallel()
        {
            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());

            UseInTransactionalScope(session => session.Save(user));

            var threadedIterations = 5;
            var delayEachTransactionBy = TimeSpanExtensions.Milliseconds(10);

            void ReadUserHistory()
            {
                UseInTransactionalScope(session =>
                                        {
                                            ((IEventStoreReader)session).GetHistory(user.Id);
                                            Thread.Sleep(delayEachTransactionBy);
                                        });
            }

            ReadUserHistory();//one warmup to get consistent times later.
            var timeForSingleTransactionalRead = (int)StopwatchExtensions.TimeExecution(ReadUserHistory).TotalMilliseconds;

            var approximateSinglethreadedExecutionTimeInMilliseconds = threadedIterations * timeForSingleTransactionalRead;

            var timingsSummary = TimeAsserter.ExecuteThreaded(
                action: ReadUserHistory,
                iterations: threadedIterations,
                timeIndividualExecutions:true,
                maxTotal: TimeSpanExtensions.Milliseconds((approximateSinglethreadedExecutionTimeInMilliseconds / 2)),
                description: $"If access is serialized the time will be approximately {approximateSinglethreadedExecutionTimeInMilliseconds} milliseconds. If parallelized it should be far below this value.");

            timingsSummary.Average.Should().BeLessThan(delayEachTransactionBy);

            timingsSummary.IndividualExecutionTimes.Sum().Should().BeGreaterThan(timingsSummary.Total);
        }

        [Test]
        public void EventsArePublishedImmediatelyOnAggregateChanges()
        {
            var users = 1.Through(9).Select(i => { var u = new User(); u.Register(i + "@test.com", "abcd", Guid.NewGuid()); u.ChangeEmail("new" + i + "@test.com"); return u; }).ToList();

            UseInTransactionalScope(session =>
                                    {
                                        users.Take(3).ForEach(session.Save);
                                        Assert.That(_eventSpy.DispatchedMessages.Count, Is.EqualTo(6));
                                    });

            UseInTransactionalScope(session =>
                                    {
                                        Assert.That(_eventSpy.DispatchedMessages.Count, Is.EqualTo(6));
                                        users.Skip(3).Take(3).ForEach(session.Save);
                                        Assert.That(_eventSpy.DispatchedMessages.Count, Is.EqualTo(12));
                                    });

            UseInTransactionalScope(session =>
                                    {
                                        Assert.That(_eventSpy.DispatchedMessages.Count, Is.EqualTo(12));
                                        users.Skip(6).Take(3).ForEach(session.Save);
                                        Assert.That(_eventSpy.DispatchedMessages.Count, Is.EqualTo(18));
                                    });

            UseInTransactionalScope(session =>
                                    {
                                        Assert.That(_eventSpy.DispatchedMessages.Count, Is.EqualTo(18));
                                        Assert.That(_eventSpy.DispatchedMessages.OfType<IAggregateEvent>().Select(e => e.EventId).Distinct().Count(), Is.EqualTo(18));
                                        var allPersistedEvents = ServiceLocator.EventStore().ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize();

                                        _eventSpy.DispatchedMessages.OfType<IAggregateEvent>().ShouldBeEquivalentTo(allPersistedEvents,options => options.WithStrictOrdering());
                                    });
        }

        [Test]
        public void InsertNewEventType_should_not_throw_exception_if_the_event_type_has_been_inserted_by_something_else()
        {
            User otherUser = null;
            User user = null;
            void ChangeAnotherUsersEmailInOtherInstance()
            {
                using (var clonedServiceLocator = ServiceLocator.Clone())
                {
                    clonedServiceLocator.ExecuteTransactionInIsolatedScope(() =>
                                                                          {
                                                                              // ReSharper disable once AccessToDisposedClosure
                                                                              var session = clonedServiceLocator.Resolve<ITestingEventStoreUpdater>();
                                                                              otherUser = User.Register(session,
                                                                                                        "email@email.se",
                                                                                                        "password",
                                                                                                        Guid.NewGuid());
                                                                              otherUser.ChangeEmail("otheruser@email.new");
                                                                          });

                }
            }

            UseInTransactionalScope(session => user = User.Register(session, "email@email.se", "password", Guid.NewGuid()));

            ChangeAnotherUsersEmailInOtherInstance();
            UseInScope(session => session.Get<User>(otherUser.Id).Email.Should().Be("otheruser@email.new"));

            UseInTransactionalScope(session => user.ChangeEmail("some@email.new"));
        }


        [Test] public void If_the_first_transaction_to_insert_an_event_of_specific_type_fails_the_next_succeeds()
        {
            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());

            UseInTransactionalScope(session => session.Save(user));

            void ChangeUserEmail(bool failOnPrepare)
            {
                UseInTransactionalScope(session =>
                {
                    if(failOnPrepare)
                    {
                        Transaction.Current.FailOnPrepare();
                    }
                    var loadedUser = session.Get<User>(user.Id);
                    loadedUser.ChangeEmail("new@email.com");
                });
            }

            AssertThrows.Exception<Exception>(() => ChangeUserEmail(failOnPrepare: true));

            ChangeUserEmail(failOnPrepare: false);
        }
    }
}