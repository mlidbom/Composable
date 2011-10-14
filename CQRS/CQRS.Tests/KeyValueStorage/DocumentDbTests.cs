using System;
using System.Collections.Generic;
using System.Threading;
using System.Transactions;
using Composable.KeyValueStorage;
using Composable.SystemExtensions.Threading;
using NUnit.Framework;
using System.Linq;

namespace CQRS.Tests.KeyValueStorage
{
    [TestFixture]
    public abstract class DocumentDbTests
    {
        protected abstract IDocumentDb CreateStore();


        [Test]
        public void CanSaveAndLoadAggregate()
        {
            var store = CreateStore();

            var user = new User()
                           {
                               Id = Guid.NewGuid(),
                               Email = "email@email.se",
                               Password = "password",
                               Address = new Address()
                                             {
                                                 City = "Stockholm", Street = "Brännkyrkag", Streetnumber=234
                                             }
                           };

            using (var session = store.OpenSession())
            {
                session.Save(user.Id, user);
                session.SaveChanges();
            }

            using (var session = store.OpenSession())
            {
                var loadedUser = session.Get<User>(user.Id);

                Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
                Assert.That(loadedUser.Email, Is.EqualTo(user.Email));
                Assert.That(loadedUser.Password, Is.EqualTo(user.Password));

                Assert.That(loadedUser.Address, Is.EqualTo(user.Address));
            }
        }

        [Test]
        public void CanSaveAndLoadAggregateForUpdate()
        {
            var store = CreateStore();

            var user = new User()
            {
                Id = Guid.NewGuid(),
                Email = "email@email.se",
                Password = "password",
                Address = new Address()
                {
                    City = "Stockholm",
                    Street = "Brännkyrkag",
                    Streetnumber = 234
                }
            };

            using (var session = store.OpenSession())
            {
                session.Save(user.Id, user);
                session.SaveChanges();
            }

            using (var session = store.OpenSession())
            {
                var loadedUser = session.GetForUpdate<User>(user.Id);

                Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
                Assert.That(loadedUser.Email, Is.EqualTo(user.Email));
                Assert.That(loadedUser.Password, Is.EqualTo(user.Password));

                Assert.That(loadedUser.Address, Is.EqualTo(user.Address));
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
                var loaded1 = session.Get<User>(user.Id);
                var loaded2 = session.Get<User>(user.Id);
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

                var loaded1 = session.Get<User>(user.Id);
                var loaded2 = session.Get<User>(user.Id);
                Assert.That(loaded1, Is.SameAs(loaded2));
                Assert.That(loaded1, Is.SameAs(user));

                session.SaveChanges();
            }
        }

        [Test]
        public void HandlesHashSets()
        {
            var store = CreateStore();

            var user = new User() { Id = Guid.NewGuid() };
            var userSet = new HashSet<User>() { user };

            using (var session = store.OpenSession())
            {
                session.Save(user.Id, userSet);
                session.SaveChanges();
            }

            using (var session = store.OpenSession())
            {
                var loadedUser = session.Get<HashSet<User>>(user.Id);
                Assert.That(loadedUser.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void HandlesHashSetsInObjects()
        {
            var store = CreateStore();

            var userInSet = new User()
                                {
                                    Id = Guid.NewGuid(),
                                    Email = "Email"
                                    
                                };

            var user = new User() { 
                Id = Guid.NewGuid(),
                People = new HashSet<User>() { userInSet }
            };;

            using (var session = store.OpenSession())
            {
                session.Save(user.Id, user);
                session.SaveChanges();
            }

            using (var session = store.OpenSession())
            {
                var loadedUser = session.Get<User>(user.Id);
                Assert.That(loadedUser.People.Count, Is.EqualTo(1));
                var loadedUserInSet = loadedUser.People.Single();
                Assert.That(loadedUserInSet.Id, Is.EqualTo(userInSet.Id));
            }
        }


        [Test]
        public void ThrowsExceptionWhenAttemptingToDeleteNonExistingValue()
        {
            var store = CreateStore();
            using(var session = store.OpenSession())
            {
                Assert.Throws<NoSuchDocumentException>(() => session.Delete(new Dog()));
            }            
        }

        [Test]
        public void HandlesDeletesOfInstancesAlreadyLoaded()
        {
            var store = CreateStore();
            var user = new User() {Id = Guid.NewGuid()};
            
            using(var session = store.OpenSession())
            {
                session.Save(user);
                session.SaveChanges();
            }

            using(var session = store.OpenSession())
            {
                var loadedUser = session.Get<User>(user.Id);
                session.Delete(user);
                session.SaveChanges();

                Assert.Throws<NoSuchDocumentException>(() => session.Get<User>(user.Id));
            }

            using (var session = store.OpenSession())
            {
                Assert.Throws<NoSuchDocumentException>(() => session.Get<User>(user.Id));
            }
        }

        [Test]
        public void HandlesDeletesOfInstancesNotYetLoaded()
        {
            var store = CreateStore();
            var user = new User() { Id = Guid.NewGuid() };

            using (var session = store.OpenSession())
            {
                session.Save(user);
                session.SaveChanges();
            }

            using (var session = store.OpenSession())
            {
                session.Delete(user);
                session.SaveChanges();

                Assert.Throws<NoSuchDocumentException>(() => session.Get<User>(user.Id));
            }

            using (var session = store.OpenSession())
            {
                Assert.Throws<NoSuchDocumentException>(() => session.Get<User>(user.Id));
            }
        }

        [Test]
        public void HandlesAValueBeingAddedAndDeletedDuringTheSameSession()
        {
            var store = CreateStore();
            var user = new User() { Id = Guid.NewGuid() };

            using (var session = store.OpenSession())
            {
                session.Save(user);
                session.Delete(user);
                session.SaveChanges();
                Assert.Throws<NoSuchDocumentException>(() => session.Get<User>(user.Id));
            }

            using (var session = store.OpenSession())
            {
                Assert.Throws<NoSuchDocumentException>(() => session.Get<User>(user.Id));
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
                var loadedUser = session.Get<User>(user.Id);
                loadedUser.Password = "NewPassword";
                session.SaveChanges();
            }

            using (var session = store.OpenSession())
            {
                var loadedUser = session.Get<User>(user.Id);
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

        [Test]
        public void HandlesInstancesOfDifferentTypesWithTheSameId()
        {
            var store = CreateStore();

            var user = new User()
            {
                Id = Guid.NewGuid(),
                Email = "email"
            };

            var dog = new Dog() { Id = user.Id };

            using (var session = store.OpenSession())
            {
                session.Save(user);
                session.Save(dog);
                session.SaveChanges();
            }

            using (var session = store.OpenSession())
            {
                var loadedDog = session.Get<Dog>(dog.Id);
                var loadedUser = session.Get<User>(dog.Id);

                Assert.That(loadedDog.Name, Is.EqualTo(dog.Name));
                Assert.That(loadedUser.Email, Is.EqualTo(user.Email));
                Assert.That(loadedDog.Id, Is.EqualTo(user.Id));
                Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
            }
        }


        [Test]
        public void FetchesAllinstancesPerType()
        {
            var store = CreateStore();

            using (var session = store.OpenSession())
            {
                session.Save(new User(){ Id = Guid.NewGuid() });
                session.Save(new User() { Id = Guid.NewGuid() });
                session.Save(new Dog() { Id = Guid.NewGuid() });
                session.Save(new Dog() { Id = Guid.NewGuid() });
                session.SaveChanges();
            }

            using (var session = store.OpenSession())
            {
                Assert.That(session.GetAll<Dog>().ToList(), Has.Count.EqualTo(2));
                Assert.That(session.GetAll<User>().ToList(), Has.Count.EqualTo(2));
            }
        }

        [Test]
        public void ThrowsIfUsedByMultipleThreads()
        {
            var store = CreateStore();
            IDocumentDbSession session = null;
            var wait = new ManualResetEvent(false);
            ThreadPool.QueueUserWorkItem((state) =>
            {
                session = store.OpenSession();
                wait.Set();
            });
            wait.WaitOne();

            User user = new User();

            Assert.Throws<MultiThreadedUseException>(() => session.Get<User>(Guid.NewGuid()));
            Assert.Throws<MultiThreadedUseException>(() => session.GetAll<User>());
            Assert.Throws<MultiThreadedUseException>(() => session.Save(user, user.Id));            
            Assert.Throws<MultiThreadedUseException>(() => session.Delete(user));
            Assert.Throws<MultiThreadedUseException>(() => session.Dispose());            
            Assert.Throws<MultiThreadedUseException>(() => session.Save(new User()));
            Assert.Throws<MultiThreadedUseException>(() => session.SaveChanges());
            Assert.Throws<MultiThreadedUseException>(() => session.TryGet(Guid.NewGuid(), out user));
            Assert.Throws<MultiThreadedUseException>(() => session.TryGetForUpdate(user.Id, out user));

        }


        [Test]
        public void GetHandlesSubTyping()
        {
            var store = CreateStore();

            var user1 = new User() { Id = Guid.NewGuid() };
            var person1 = new Person() { Id = Guid.NewGuid() };

            using (var session = store.OpenSession())
            {                
                session.Save(user1);                
                session.Save(person1);
                session.SaveChanges();
            }

            using (var session = store.OpenSession())
            {
                Assert.That(session.Get<Person>(user1.Id), Is.EqualTo(user1));
                Assert.That(session.Get<Person>(person1.Id), Is.EqualTo(person1));
            }
        }

        [Test]
        public void GetAllHandlesSubTyping()
        {
            var store = CreateStore();

            var user1 = new User() { Id = Guid.NewGuid() };
            var person1 = new Person() { Id = Guid.NewGuid() };

            using (var session = store.OpenSession())
            {
                session.Save(user1);
                session.Save(person1);
                session.SaveChanges();
            }

            using (var session = store.OpenSession())
            {
                var people = session.GetAll<Person>().ToList();

                Assert.That(people, Has.Count.EqualTo(2));
                Assert.That(people, Contains.Item(user1));
                Assert.That(people, Contains.Item(person1));
            }
        }



        //[Test]
        //public void SaveChangesAndCommitWhenTransientTransactionDoesSo()
        //{
        //    var store = CreateStore();

        //    var user = new User()
        //                   {
        //                       Id = Guid.NewGuid(),
        //                       Email = "email@email.se",
        //                       Password = "password"
        //                   };

        //    using (var session = store.OpenSession())
        //    {
        //        using (var transaction = new TransactionScope())
        //        {
        //            session.Save(user);
        //            transaction.Complete();
        //        }
        //    }

        //    using (var session = store.OpenSession())
        //    {
        //        var loadedUser = session.Get<User>(user.Id);

        //        Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
        //        Assert.That(loadedUser.Email, Is.EqualTo(user.Email));
        //        Assert.That(loadedUser.Password, Is.EqualTo(user.Password));

        //    }
        //}

        //[Test]
        //public void SaveChangesAndCommitWhenTransientTransactionDoesSoWhenCreatedAndDisposedWithinTransaction()
        //{
        //    var store = CreateStore();

        //    var user = new User()
        //    {
        //        Id = Guid.NewGuid(),
        //        Email = "email@email.se",
        //        Password = "password"
        //    };

        //    using (var transaction = new TransactionScope())
        //    {
        //        using(var session = store.OpenSession())
        //        {
        //            session.Save(user);
        //            transaction.Complete();
        //        }
        //    }

        //    using (var session = store.OpenSession())
        //    {
        //        var loadedUser = session.Get<User>(user.Id);

        //        Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
        //        Assert.That(loadedUser.Email, Is.EqualTo(user.Email));
        //        Assert.That(loadedUser.Password, Is.EqualTo(user.Password));

        //    }
        //}
    }
}