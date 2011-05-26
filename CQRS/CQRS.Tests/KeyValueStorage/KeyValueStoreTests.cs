using System;
using Composable.KeyValueStorage;
using NUnit.Framework;

namespace CQRS.Tests.KeyValueStorage
{
    [TestFixture]
    public abstract class KeyValueStoreTests
    {
        protected abstract IKeyValueStore CreateStore();


        [Test]
        public void CanSaveAndLoadAggregate()
        {
            var store = CreateStore();

            var user = new User()
                           {
                               Id = Guid.NewGuid(),
                               Email = "email@email.se",
                               Password = "password"
                           };

            using (var session = store.OpenSession())
            {
                session.Save(user.Id, user);
                session.SaveChanges();
            }

            using (var session = store.OpenSession())
            {
                var loadedUser = session.Load<User>(user.Id);

                Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
                Assert.That(loadedUser.Email, Is.EqualTo(user.Email));
                Assert.That(loadedUser.Password, Is.EqualTo(user.Password));

            }
        }

        
        [Test]
        public void ReturnsSameInstanceOnRepeatedLoads()
        {
            var store = CreateStore();

            var user = new User() { Id = Guid.NewGuid()};

            using (var session = store.OpenSession())
            {
                session.Save(user.Id, user);
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

            var user = new User() { Id = Guid.NewGuid() };

            using (var session = store.OpenSession())
            {
                session.Save(user.Id, user);

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

            var user = new User() { Id = Guid.NewGuid() };

            using (var session = store.OpenSession())
            {
                session.Save(user.Id, user);
                session.SaveChanges();
            }

            using (var session = store.OpenSession())
            {
                var loadedUser = session.Load<User>(user.Id);
                loadedUser.Password = "NewPassword";
                session.SaveChanges();
            }

            using (var session = store.OpenSession())
            {
                var loadedUser = session.Load<User>(user.Id);
                Assert.That(loadedUser.Password, Is.EqualTo("NewPassword"));
            }
        }

        [Test]
        public void ThrowsWhenAttemptingToSaveExistingAggregate()
        {
            var store = CreateStore();

            var user = new User() { Id = Guid.NewGuid() };

            using (var session = store.OpenSession())
            {
                session.Save(user.Id, user);
                session.SaveChanges();
            }

            Assert.Throws<AttemptToSaveAlreadyPersistedValueException>(() =>
            {
                using (var session = store.OpenSession())
                {
                    session.Save(user.Id, user);
                    session.SaveChanges();
                }
            });
        }
    }
}