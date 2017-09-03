using System;
using System.Linq;
using System.Threading;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Testing;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore;
using Composable.System.Diagnostics;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using Composable.Testing;
using Composable.Testing.Performance;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Composable.CQRS.Tests.CQRS.EventSourcing
{
    using Composable.System;

    [TestFixture]
    public abstract class EventStoreSessionTests
    {
        IMessageSpy MessageSpy => ServiceLocator.Resolve<IMessageSpy>();

        protected abstract IServiceLocator CreateServiceLocator();
        internal IServiceLocator ServiceLocator { get; private set; }

        [SetUp] public void SetupBus()
        {
            ServiceLocator = CreateServiceLocator();
        }

        [TearDown] public void TearDownTask()
        {
            ServiceLocator.Dispose();
        }


        protected void UseInTransactionalScope([InstantHandle] Action<IEventStoreUpdater> useSession)
            => ServiceLocator.ExecuteTransactionInIsolatedScope(
                () => useSession(ServiceLocator.Resolve<ITestingEventstoreUpdater>()));

        protected void UseInScope([InstantHandle]Action<IEventStoreUpdater> useSession)
            => ServiceLocator.ExecuteInIsolatedScope(
                () => useSession(ServiceLocator.Resolve<ITestingEventstoreUpdater>()));

        [Test]
        public void WhenFetchingAggregateThatDoesNotExistNoSuchAggregateExceptionIsThrown()
        {
            UseInScope(session => Assert.Throws<AggregateRootNotFoundException>(() => session.Get<User>(Guid.NewGuid())));
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
                                                                                       updater = ServiceLocator.Resolve<ITestingEventstoreUpdater>();
                                                                                       reader = ServiceLocator.Resolve<ITestingEventstoreReader>();
                                                                                   });
                                             wait.Set();
                                         });
            wait.Wait();

            Assert.Throws<MultiThreadedUseException>(() => updater.Get<User>(Guid.NewGuid()));
            Assert.Throws<MultiThreadedUseException>(() => updater.Dispose());
            Assert.Throws<MultiThreadedUseException>(() => reader.LoadSpecificVersion<User>(Guid.NewGuid(), 1));
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
                           var reader = ServiceLocator.Resolve<ITestingEventstoreReader>();
                           var loadedUser = reader.LoadSpecificVersion<User>(user.Id, 1);
                           Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
                           Assert.That(loadedUser.Email, Is.EqualTo("email@email.se"));
                           Assert.That(loadedUser.Password, Is.EqualTo("password"));

                           loadedUser = reader.LoadSpecificVersion<User>(user.Id, 2);
                           Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
                           Assert.That(loadedUser.Email, Is.EqualTo("email@email.se"));
                           Assert.That(loadedUser.Password, Is.EqualTo("NewPassword"));

                           loadedUser = reader.LoadSpecificVersion<User>(user.Id, 3);
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
                                        var loadedUser = ServiceLocator.Resolve<ITestingEventstoreReader>().LoadSpecificVersion<User>(user.Id, 1);
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

        [Test, Ignore("TODO. Fix this long standing design issue. This test will probably be removed because get changes is refactored out of existance as we force the store updater to create the instances and it will immediately track updates via observables")]
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
        public void AggregateCannotBeRetreivedAfterBeingDeleted()
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

            MessageSpy.DispatchedMessages.Count().Should().Be(2);

            UseInTransactionalScope(session =>
                                    {
                                        user1 = session.Get<User>(user1.Id);

                                        user1.ChangeEmail("new_email");

                                        session.Delete(user1.Id);
                                    });

            var published = MessageSpy.DispatchedMessages.ToList();
            MessageSpy.DispatchedMessages.Count()
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

                                        MessageSpy.DispatchedMessages.Last()
                                                  .Should()
                                                  .BeOfType<UserRegistered>();

                                        user1 = session.Get<User>(user1.Id);
                                        user1.ChangeEmail("new_email");
                                        MessageSpy.DispatchedMessages.Last()
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
                           Assert.That(history.Count(), Is.EqualTo(2));
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
                           Assert.That(history.Count(), Is.EqualTo(0));
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
                           Assert.That(history.Count(), Is.EqualTo(0));
                       });
        }


        [Test]
        public void Concurrent_read_only_access_to_aggregate_history_can_occur_in_paralell()
        {
            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());

            UseInTransactionalScope(session => session.Save(user));

            var threadedIterations = 5;
            var delayEachTransactionBy = 10.Milliseconds();

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
                maxTotal: (approximateSinglethreadedExecutionTimeInMilliseconds / 2).Milliseconds(),
                description: $"If access is serialized the time will be approximately {approximateSinglethreadedExecutionTimeInMilliseconds} milliseconds. If parelellized it should be far below this value.");

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
                                        Assert.That(MessageSpy.DispatchedMessages.Count, Is.EqualTo(6));
                                    });

            UseInTransactionalScope(session =>
                                    {
                                        Assert.That(MessageSpy.DispatchedMessages.Count, Is.EqualTo(6));
                                        users.Skip(3).Take(3).ForEach(session.Save);
                                        Assert.That(MessageSpy.DispatchedMessages.Count, Is.EqualTo(12));
                                    });

            UseInTransactionalScope(session =>
                                    {
                                        Assert.That(MessageSpy.DispatchedMessages.Count, Is.EqualTo(12));
                                        users.Skip(6).Take(3).ForEach(session.Save);
                                        Assert.That(MessageSpy.DispatchedMessages.Count, Is.EqualTo(18));
                                    });

            UseInTransactionalScope(session =>
                                    {
                                        Assert.That(MessageSpy.DispatchedMessages.Count, Is.EqualTo(18));
                                        Assert.That(MessageSpy.DispatchedMessages.OfType<IAggregateRootEvent>().Select(e => e.EventId).Distinct().Count(), Is.EqualTo(18));
                                        var allPersistedEvents = ServiceLocator.EventStore().ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize();

                                        MessageSpy.DispatchedMessages.ShouldBeEquivalentTo(allPersistedEvents,options => options.WithStrictOrdering());
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
                                                                              var session = clonedServiceLocator.Resolve<ITestingEventstoreUpdater>();
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

    }
}