using System;
using System.Transactions;
using Composable.CQRS.EventSourcing;
using NUnit.Framework;

namespace CQRS.Tests.CQRS.EventSourcing
{
    [TestFixture]
    public abstract class EventStoreTests
    {
        [Test]
        public void CanSaveAndLoadAggregate()
        {
            var store = CreateStore();
            
            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());
            user.ChangePassword("NewPassword");
            user.ChangeEmail("NewEmail");

            using(var session = store.OpenSession())
            {                
                session.Save(user);
                session.SaveChanges();
            }

            using(var session = store.OpenSession())
            {
                var loadedUser = session.Load<User>(user.Id);

                Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
                Assert.That(loadedUser.Email, Is.EqualTo(user.Email));
                Assert.That(loadedUser.Password, Is.EqualTo(user.Password));

            }
        }

        protected abstract IEventStore CreateStore();

        [Test]
        public void CanLoadSpecificVersionOfAggregate()
        {
            var store = CreateStore();
            
            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());
            user.ChangePassword("NewPassword");
            user.ChangeEmail("NewEmail");

            using (var session = store.OpenSession())
            {
                session.Save(user);
                session.SaveChanges();
            }

            using (var session = store.OpenSession())
            {
                var loadedUser = session.LoadSpecificVersion<User>(user.Id, 1);
                Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
                Assert.That(loadedUser.Email, Is.EqualTo("email@email.se"));
                Assert.That(loadedUser.Password, Is.EqualTo("password"));

                loadedUser = session.LoadSpecificVersion<User>(user.Id, 2);
                Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
                Assert.That(loadedUser.Email, Is.EqualTo("email@email.se"));
                Assert.That(loadedUser.Password, Is.EqualTo("NewPassword"));

                loadedUser = session.LoadSpecificVersion<User>(user.Id, 3);
                Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
                Assert.That(loadedUser.Email, Is.EqualTo("NewEmail"));
                Assert.That(loadedUser.Password, Is.EqualTo("NewPassword"));
            }
        }

        [Test]
        public void ReturnsSameInstanceOnRepeatedLoads()
        {
            var store = CreateStore();

            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());

            using (var session = store.OpenSession())
            {
                session.Save(user);
                session.SaveChanges();
            }

            using (var session = store.OpenSession())
            {
                var loaded1 = session.Load<User>(user.Id);
                var loaded2 = session.Load<User>(user.Id);
                Assert.That(loaded1, Is.SameAs(loaded2));
            }
        }

        [Test]
        public void ReturnsSameInstanceOnLoadAfterSave()
        {
            var store = CreateStore();

            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());

            using (var session = store.OpenSession())
            {
                session.Save(user);

                var loaded1 = session.Load<User>(user.Id);
                var loaded2 = session.Load<User>(user.Id);
                Assert.That(loaded1, Is.SameAs(loaded2));
                Assert.That(loaded1, Is.SameAs(user));

                session.SaveChanges();
            }
        }

        [Test]
        public void TracksAndUpdatesLoadedAggregates()
        {
            var store = CreateStore();

            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());

            using (var session = store.OpenSession())
            {
                session.Save(user);
                session.SaveChanges();
            }

            using (var session = store.OpenSession())
            {
                var loadedUser = session.Load<User>(user.Id);
                loadedUser.ChangePassword("NewPassword");
                session.SaveChanges();
            }

            using (var session = store.OpenSession())
            {
                var loadedUser = session.Load<User>(user.Id);
                Assert.That(loadedUser.Password, Is.EqualTo("NewPassword"));
            }
        }

        [Test]
        public void DoesNotUpdateAggregatesLoadedViaSpecificVersion()
        {
            var store = CreateStore();

            var user = new User();
            user.Register("OriginalEmail", "password", Guid.NewGuid());

            using (var session = store.OpenSession())
            {
                session.Save(user);
                session.SaveChanges();
            }

            using (var session = store.OpenSession())
            {
                var loadedUser = session.LoadSpecificVersion<User>(user.Id, 1);                
                loadedUser.ChangeEmail("NewEmail");
                session.SaveChanges();
            }

            using (var session = store.OpenSession())
            {
                var loadedUser = session.Load<User>(user.Id);
                Assert.That(loadedUser.Email, Is.EqualTo("OriginalEmail"));
            }
        }

        [Test]
        public void ResetsAggregatesAfterSaveChanges()
        {
            var store = CreateStore();

            var user = new User();
            user.Register("OriginalEmail", "password", Guid.NewGuid());

            using (var session = store.OpenSession())
            {
                session.Save(user);
                session.SaveChanges();
                Assert.That((user as IEventStored).GetChanges(), Is.Empty);
            }
        }

        [Test]
        public void ThrowsWhenAttemptingToSaveExistingAggregate()
        {
            var store = CreateStore();

            var user = new User();
            user.Register("OriginalEmail", "password", Guid.NewGuid());

            using (var session = store.OpenSession())
            {
                session.Save(user);
                session.SaveChanges();
            }

            Assert.Throws<AttemptToSaveAlreadyPersistedAggregateException>(() =>
                                                                               {
                                                                                   using(var session = store.OpenSession())
                                                                                   {
                                                                                       session.Save(user);
                                                                                       session.SaveChanges();
                                                                                   }
                                                                               });
        }

        [Test]
        public void SaveChangesAndCommitWhenTransientTransactionDoesSo()
        {
            var store = CreateStore();
            
            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());
            user.ChangePassword("NewPassword");
            user.ChangeEmail("NewEmail");

            using(var session = store.OpenSession())
            {
                using (var transaction = new TransactionScope())
                {
                    session.Save(user);
                    transaction.Complete();
                }
            }

            using(var session = store.OpenSession())
            {
                var loadedUser = session.Load<User>(user.Id);

                Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
                Assert.That(loadedUser.Email, Is.EqualTo(user.Email));
                Assert.That(loadedUser.Password, Is.EqualTo(user.Password));

            }
        }
    }
}