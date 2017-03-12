using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Composable.CQRS.KeyValueStorage;
using Composable.CQRS.UnitsOfWork;
using Composable.DDD;
using Composable.Persistence.KeyValueStorage;
using Composable.System;
using Composable.System.Linq;
using Composable.System.Web;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests.KeyValueStorage
{
    [TestFixture]
    public abstract class DocumentDbTests
    {
        protected abstract IDocumentDb CreateStore();

        protected static IDocumentDbSession OpenSession(IDocumentDb store, ISingleContextUseGuard guard = null)
        {
            return new DocumentDbSession(store, guard ?? new SingleThreadUseGuard(), NullOpDocumentDbSessionInterceptor.Instance);
        }

        [Test]
        public void CanSaveAndLoadAggregate()
        {
            var store = CreateStore();

            var user = new User
                       {
                           Id = Guid.NewGuid(),
                           Email = "email@email.se",
                           Password = "password",
                           Address = new Address
                                     {
                                         City = "Stockholm",
                                         Street = "Brännkyrkag",
                                         Streetnumber = 234
                                     }
                       };

            using (var session = OpenSession(store))
            {
                session.Save(user.Id, user);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                var loadedUser = session.Get<User>(user.Id);

                Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
                Assert.That(loadedUser.Email, Is.EqualTo(user.Email));
                Assert.That(loadedUser.Password, Is.EqualTo(user.Password));

                Assert.That(loadedUser.Address, Is.EqualTo(user.Address));
            }
        }


        [Test]
        public void GetAllWithIdsReturnsAsManyResultsAsPassedIds()
        {
            var store = CreateStore();

            var ids = 1.Through(9).Select(index => Guid.Parse("00000000-0000-0000-0000-00000000000{0}".FormatWith(index))).ToArray();

            var users = ids.Select(id => new User() { Id = id }).ToArray();

            using (var session = OpenSession(store))
            {
                users.ForEach(user => session.Save(user));
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                var fetchedById = session.Get<User>(ids.Take(5));
                fetchedById.Select(fetched => fetched.Id).Should().Equal(ids.Take(5));
            }
        }

        [Test]
        public void GetAllWithIdsThrowsNoSuchDocumentExceptionExceptionIfAnyIdIsMissing()
        {
            var store = CreateStore();

            var ids = 1.Through(9).Select(index => Guid.Parse("00000000-0000-0000-0000-00000000000{0}".FormatWith(index))).ToArray();

            var users = ids.Select(id => new User() { Id = id }).ToArray();

            using (var session = OpenSession(store))
            {
                users.ForEach(user => session.Save(user));
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                Assert.Throws<NoSuchDocumentException>(() => session.Get<User>(ids.Take(5).Append(Guid.Parse("00000000-0000-0000-0000-000000000099")).ToArray()).ToArray());
            }
        }


        [Test]
        public void GetAllWithIdsReturnsTheSameInstanceForAnyPreviouslyFetchedDocuments()
        {
            var store = CreateStore();

            var ids = 1.Through(9).Select(index => Guid.Parse("00000000-0000-0000-0000-00000000000{0}".FormatWith(index))).ToArray();

            var users = ids.Select(id => new User() { Id = id }).ToArray();

            using (var session = OpenSession(store))
            {
                users.ForEach(user => session.Save(user));
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                var fetchedIndividually = ids.Select(id => session.Get<User>(id)).ToArray();
                var fetchedWithGetAll = session.Get<User>(ids).ToArray();

                fetchedIndividually.ForEach((user, index) => Assert.That(user, Is.SameAs(fetchedWithGetAll[index])));
            }
        }



        [Test]
        public void CanSaveAndLoadAggregateForUpdate()
        {
            var store = CreateStore();

            var user = new User
                       {
                           Id = Guid.NewGuid(),
                           Email = "email@email.se",
                           Password = "password",
                           Address = new Address
                                     {
                                         City = "Stockholm",
                                         Street = "Brännkyrkag",
                                         Streetnumber = 234
                                     }
                       };

            using (var session = OpenSession(store))
            {
                session.Save(user.Id, user);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                var loadedUser = session.GetForUpdate<User>(user.Id);

                Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
                Assert.That(loadedUser.Email, Is.EqualTo(user.Email));
                Assert.That(loadedUser.Password, Is.EqualTo(user.Password));

                Assert.That(loadedUser.Address, Is.EqualTo(user.Address));
            }
        }

        [Test]
        public void CallingSaveWithAnInteraceAsTypeParameterDoesNotExplode()
        {
            var store = CreateStore();

            IPersistentEntity<Guid> user1 = new User { Id = Guid.NewGuid(), Email = "user1" };
            IPersistentEntity<Guid> user2 = new User { Id = Guid.NewGuid(), Email = "user2" };

            using (var session = OpenSession(store))
            {
                session.Save(user2);
                session.Save(user1.Id, user1);
                session.Get<User>(user1.Id).Should().Be(user1);
                session.Get<User>(user2.Id).Should().Be(user2);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                session.Get<User>(user1.Id).Id.Should().Be(user1.Id);
                session.Get<User>(user2.Id).Id.Should().Be(user2.Id);
                session.SaveChanges();
            }
        }

        [Test]
        public void AddingAndRemovingObjectResultsInNoObjectBeingSaved()
        {
            var store = CreateStore();

            var user = new User { Id = Guid.NewGuid() };

            using (var session = OpenSession(store))
            {
                session.Save(user.Id, user);
                session.Delete(user);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                session.TryGet(user.Id, out user).Should().BeFalse();
            }
        }


        [Test]
        public void AddingAndRemovingObjectInUnitOfWorkResultsInNoObjectBeingSaved()
        {
            var store = CreateStore();

            var user = new User { Id = Guid.NewGuid() };

            using (var session = OpenSession(store))
            {
                var uow = new UnitOfWork(new SingleThreadUseGuard());
                uow.AddParticipant((IUnitOfWorkParticipant)session);

                session.Save(user.Id, user);
                session.Delete(user);

                uow.Commit();
            }

            using (var session = OpenSession(store))
            {
                session.TryGet(user.Id, out user).Should().BeFalse();
            }
        }

        [Test]
        public void AddingRemovingAndAddingObjectInUnitOfWorkResultsInNoObjectBeingSaved()
        {
            var store = CreateStore();

            var user = new User { Id = Guid.NewGuid() };

            using (var session = OpenSession(store))
            {
                var uow = new UnitOfWork(new SingleThreadUseGuard());
                uow.AddParticipant((IUnitOfWorkParticipant)session);

                session.Save(user.Id, user);
                session.Delete(user);
                session.Save(user.Id, user);

                uow.Commit();
            }

            using (var session = OpenSession(store))
            {
                session.TryGet(user.Id, out user).Should().BeTrue();
            }
        }

        [Test]
        public void ObjectsWhoseKeysDifferOnlyByCaseAreConsideredTheSameObjectForCompatabilityWithSqlServer()
        {
            var store = CreateStore();

            var lowerCase = new Email("theemail");
            var upperCase = new Email(lowerCase.TheEmail.ToUpper());

            using (var session = OpenSession(store))
            {
                var uow = new UnitOfWork(new SingleThreadUseGuard());
                uow.AddParticipant((IUnitOfWorkParticipant)session);

                session.Save(lowerCase.TheEmail, lowerCase);
                Assert.Throws<AttemptToSaveAlreadyPersistedValueException>(() => session.Save(upperCase.TheEmail, upperCase));

                session.Get<Email>(lowerCase.TheEmail).Should().Be(session.Get<Email>(upperCase.TheEmail));

                uow.Commit();
            }

            using (var session = OpenSession(store))
            {
                var uow = new UnitOfWork(new SingleThreadUseGuard());
                uow.AddParticipant((IUnitOfWorkParticipant)session);

                Assert.Throws<AttemptToSaveAlreadyPersistedValueException>(() => session.Save(upperCase.TheEmail, upperCase));
                session.Get<Email>(upperCase.TheEmail).TheEmail.Should().Be(lowerCase.TheEmail);
                session.Get<Email>(lowerCase.TheEmail).Should().Be(session.Get<Email>(upperCase.TheEmail));

                session.Delete<Email>(upperCase.TheEmail);
                Assert.Throws<NoSuchDocumentException>(() => session.Delete<Email>(upperCase.TheEmail));
                Assert.Throws<NoSuchDocumentException>(() => session.Delete<Email>(lowerCase.TheEmail));


                uow.Commit();
            }
        }

        [Test]
        public void ObjectsWhoseKeysDifferOnlyByTrailingSpacesTrailingWhiteSpaceCaseAreConsideredTheSameObjectForCompatabilityWithSqlServer()
        {
            var store = CreateStore();

            var noWhitespace = new Email("theemail");
            var withWhitespace = new Email(noWhitespace.TheEmail + "  ");

            using (var session = OpenSession(store))
            {
                var uow = new UnitOfWork(new SingleThreadUseGuard());
                uow.AddParticipant((IUnitOfWorkParticipant)session);

                session.Save(noWhitespace.TheEmail, noWhitespace);
                Assert.Throws<AttemptToSaveAlreadyPersistedValueException>(() => session.Save(withWhitespace.TheEmail, withWhitespace));

                session.Get<Email>(noWhitespace.TheEmail).Should().Be(session.Get<Email>(withWhitespace.TheEmail));

                uow.Commit();
            }

            using (var session = OpenSession(store))
            {
                var uow = new UnitOfWork(new SingleThreadUseGuard());
                uow.AddParticipant((IUnitOfWorkParticipant)session);

                Assert.Throws<AttemptToSaveAlreadyPersistedValueException>(() => session.Save(withWhitespace.TheEmail, withWhitespace));
                session.Get<Email>(withWhitespace.TheEmail).TheEmail.Should().Be(noWhitespace.TheEmail);
                session.Get<Email>(noWhitespace.TheEmail).Should().Be(session.Get<Email>(withWhitespace.TheEmail));

                session.Delete<Email>(withWhitespace.TheEmail);
                Assert.Throws<NoSuchDocumentException>(() => session.Delete<Email>(withWhitespace.TheEmail));
                Assert.Throws<NoSuchDocumentException>(() => session.Delete<Email>(noWhitespace.TheEmail));


                uow.Commit();
            }
        }

        [Test]
        public void TryingToFetchNonExistentItemDoesNotCauseSessionToTryAndAddItWithANullInstance()
        {
            var store = CreateStore();

            var user = new User { Id = Guid.NewGuid() };

            using (var session = OpenSession(store))
            {
                session.TryGet(user.Id, out user);
                session.SaveChanges();
            }
        }

        [Test]
        public void RepeatedlyAddingAndRemovingObjectResultsInNoObjectBeingSaved()
        {
            var store = CreateStore();

            var user = new User { Id = Guid.NewGuid() };

            using (var session = OpenSession(store))
            {
                session.Save(user.Id, user);
                session.Delete(user);
                session.Save(user.Id, user);
                session.Delete(user);
                session.Save(user.Id, user);
                session.Delete(user);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                session.TryGet(user.Id, out user).Should().BeFalse();
            }
        }

        [Test]
        public void LoadingRemovingAndAddingObjectInUnitOfWorkResultsInObjectBeingSaved()
        {
            var store = CreateStore();

            var user = new User { Id = Guid.NewGuid() };

            using (var session = OpenSession(store))
            {
                session.Save(user.Id, user);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                var uow = new UnitOfWork(new SingleThreadUseGuard());
                uow.AddParticipant((IUnitOfWorkParticipant)session);

                user = session.Get<User>(user.Id);
                session.Delete(user);

                User tmpUser;
                session.TryGet(user.Id, out tmpUser).Should().Be(false);
                session.Save(user);
                session.TryGet(user.Id, out tmpUser).Should().Be(true);
                session.Delete(user);
                session.TryGet(user.Id, out tmpUser).Should().Be(false);
                session.Save(user);
                session.TryGet(user.Id, out tmpUser).Should().Be(true);

                uow.Commit();
            }

            using (var session = OpenSession(store))
            {
                session.TryGet(user.Id, out user).Should().Be(true);
            }
        }


        [Test]
        public void ReturnsSameInstanceOnRepeatedLoads()
        {
            var store = CreateStore();

            var user = new User { Id = Guid.NewGuid() };

            using (var session = OpenSession(store))
            {
                session.Save(user.Id, user);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
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

            var user = new User { Id = Guid.NewGuid() };

            using (var session = OpenSession(store))
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

            var user = new User { Id = Guid.NewGuid() };
            var userSet = new HashSet<User> { user };

            using (var session = OpenSession(store))
            {
                session.Save(user.Id, userSet);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                var loadedUser = session.Get<HashSet<User>>(user.Id);
                Assert.That(loadedUser.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void HandlesHashSetsInObjects()
        {
            var store = CreateStore();

            var userInSet = new User
                            {
                                Id = Guid.NewGuid(),
                                Email = "Email"
                            };

            var user = new User
                       {
                           Id = Guid.NewGuid(),
                           People = new HashSet<User> { userInSet }
                       };

            using (var session = OpenSession(store))
            {
                session.Save(user.Id, user);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
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
            using (var session = OpenSession(store))
            {
                var lassie = new Dog { Id = Guid.NewGuid() };
                var buster = new Dog { Id = Guid.NewGuid() };
                session.Save(lassie);
                session.SaveChanges();

                Assert.Throws<NoSuchDocumentException>(() =>
                                                       {
                                                           session.Delete(buster);
                                                           session.SaveChanges();
                                                       });
            }
        }

        [Test]
        public void HandlesDeletesOfInstancesAlreadyLoaded()
        {
            var store = CreateStore();
            var user = new User { Id = Guid.NewGuid() };

            using (var session = OpenSession(store))
            {
                session.Save(user);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                var loadedUser = session.Get<User>(user.Id);
                loadedUser.Should().NotBeNull();
                session.Delete(user);
                session.SaveChanges();

                Assert.Throws<NoSuchDocumentException>(() => session.Get<User>(user.Id));
            }

            using (var session = OpenSession(store))
            {
                Assert.Throws<NoSuchDocumentException>(() => session.Get<User>(user.Id));
            }
        }

        [Test]
        public void HandlesDeletesOfInstancesNotYetLoaded()
        {
            var store = CreateStore();
            var user = new User { Id = Guid.NewGuid() };

            using (var session = OpenSession(store))
            {
                session.Save(user);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                session.Delete(user);
                session.SaveChanges();

                Assert.Throws<NoSuchDocumentException>(() => session.Get<User>(user.Id));
            }

            using (var session = OpenSession(store))
            {
                Assert.Throws<NoSuchDocumentException>(() => session.Get<User>(user.Id));
            }
        }

        [Test]
        public void HandlesAValueBeingAddedAndDeletedDuringTheSameSession()
        {
            var store = CreateStore();
            var user = new User { Id = Guid.NewGuid() };

            using (var session = OpenSession(store))
            {
                session.Save(user);
                session.Delete(user);
                session.SaveChanges();
                Assert.Throws<NoSuchDocumentException>(() => session.Get<User>(user.Id));
            }

            using (var session = OpenSession(store))
            {
                Assert.Throws<NoSuchDocumentException>(() => session.Get<User>(user.Id));
            }
        }

        [Test]
        public void TracksAndUpdatesLoadedAggregates()
        {
            var store = CreateStore();

            var user = new User { Id = Guid.NewGuid() };

            using (var session = OpenSession(store))
            {
                session.Save(user.Id, user);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                var loadedUser = session.Get<User>(user.Id);
                loadedUser.Password = "NewPassword";
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                var loadedUser = session.Get<User>(user.Id);
                Assert.That(loadedUser.Password, Is.EqualTo("NewPassword"));
            }
        }

        [Test]
        public void ThrowsWhenAttemptingToSaveExistingAggregate()
        {
            var store = CreateStore();

            var user = new User { Id = Guid.NewGuid() };

            using (var session = OpenSession(store))
            {
                session.Save(user.Id, user);
                session.SaveChanges();
            }

            Assert.Throws<AttemptToSaveAlreadyPersistedValueException>(() =>
                                                                       {
                                                                           using (var session = OpenSession(store))
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

            var user = new User
                       {
                           Id = Guid.NewGuid(),
                           Email = "email"
                       };

            var dog = new Dog { Id = user.Id };

            using (var session = OpenSession(store))
            {
                session.Save<IPersistentEntity<Guid>>(user);
                session.Save<IPersistentEntity<Guid>>(dog);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
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

            using (var session = OpenSession(store))
            {
                session.Save(new User { Id = Guid.NewGuid() });
                session.Save(new User { Id = Guid.NewGuid() });
                session.Save(new Dog { Id = Guid.NewGuid() });
                session.Save(new Dog { Id = Guid.NewGuid() });
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
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
            var wait = new ManualResetEventSlim();
            ThreadPool.QueueUserWorkItem(state =>
                                         {
                                             session = OpenSession(store);
                                             wait.Set();
                                         });
            wait.Wait();

            var user = new User() { Id = Guid.NewGuid() };

            Assert.Throws<MultiThreadedUseException>(() => session.Get<User>(Guid.NewGuid()));
            Assert.Throws<MultiThreadedUseException>(() => session.GetAll<User>());
            Assert.Throws<MultiThreadedUseException>(() => session.Save(user, user.Id));
            Assert.Throws<MultiThreadedUseException>(() => session.Delete(user));
            Assert.Throws<MultiThreadedUseException>(() => session.Dispose());
            Assert.Throws<MultiThreadedUseException>(() => session.Save(new User()));
            Assert.Throws<MultiThreadedUseException>(() => session.SaveChanges());
            Assert.Throws<MultiThreadedUseException>(() => session.TryGet(Guid.NewGuid(), out user));
            Assert.Throws<MultiThreadedUseException>(() => session.TryGetForUpdate(user.Id, out user));
            Assert.Throws<MultiThreadedUseException>(() => session.Delete(user));
        }

        [Test]
        public void ThrowsIfUsedByMultipleHttpRequests()
        {
            var store = CreateStore();

            var guard = new SingleHttpRequestUseGuard(new AlwaysNewRequestIdsHttpRequestFetcher());

            var session = OpenSession(store, guard);

            var user = new User() {Id = Guid.NewGuid()};

            // ReSharper disable once NotAccessedVariable
            bool found;
            Assert.Throws<MultiRequestAccessDetected>(() => session.Get<User>(Guid.NewGuid()));
            Assert.Throws<MultiRequestAccessDetected>(() => session.GetAll<User>());
            Assert.Throws<MultiRequestAccessDetected>(() => session.Save(user, user.Id));
            Assert.Throws<MultiRequestAccessDetected>(() => session.Delete(user));
            Assert.Throws<MultiRequestAccessDetected>(session.Dispose);
            Assert.Throws<MultiRequestAccessDetected>(() => session.Save(new User()));
            Assert.Throws<MultiRequestAccessDetected>(session.SaveChanges);
            Assert.Throws<MultiRequestAccessDetected>(() => found = session.TryGet(Guid.NewGuid(), out user));
            Assert.Throws<MultiRequestAccessDetected>(() => found = session.TryGetForUpdate(user.Id, out user));
            Assert.Throws<MultiRequestAccessDetected>(() => session.Delete(user));
        }


        [Test]
        public void GetHandlesSubTyping()
        {
            var store = CreateStore();

            var user1 = new User { Id = Guid.NewGuid() };
            var person1 = new Person { Id = Guid.NewGuid() };

            using (var session = OpenSession(store))
            {
                session.Save(user1);
                session.Save(person1);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                Assert.That(session.Get<Person>(user1.Id), Is.EqualTo(user1));
                Assert.That(session.Get<Person>(person1.Id), Is.EqualTo(person1));
            }
        }

        [Test]
        public void GetAllHandlesSubTyping()
        {
            var store = CreateStore();

            var user1 = new User { Id = Guid.NewGuid() };
            var person1 = new Person { Id = Guid.NewGuid() };

            using (var session = OpenSession(store))
            {
                session.Save(user1);
                session.Save(person1);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                var people = session.GetAll<Person>().ToList();

                Assert.That(people, Has.Count.EqualTo(2));
                Assert.That(people, Contains.Item(user1));
                Assert.That(people, Contains.Item(person1));
            }
        }

        [Test]
        public void ThrowsExceptionIfYouTryToSaveAnIHasPersistentIdentityWithNoId()
        {
            var store = CreateStore();

            var user1 = new User { Id = Guid.Empty };

            using (var session = OpenSession(store))
            {
                session.Invoking(sess => sess.Save(user1))
                    .ShouldThrow<Exception>();
            }
        }

        [Test]
        public void GetByIdsShouldReturnOnlyMatchingResultEvenWhenMoreResultsAreInTheCache()
        {
            var store = CreateStore();

            var user1 = new User { Id = Guid.Parse("00000000-0000-0000-0000-000000000001") };
            var user2 = new User { Id = Guid.Parse("00000000-0000-0000-0000-000000000002") };

            using (var session = OpenSession(store))
            {
                session.Save(user1);
                session.Save(user2);

                var people = session.Get<User>(new[] { user1.Id });

                Assert.That(people.ToList(), Has.Count.EqualTo(1));
                Assert.That(people, Contains.Item(user1));
            }
        }


        [Test]
        public void GetAllIdsShouldOnlyReturnResultsWithTheGivenType()
        {
            var readingDocumentDb = CreateStore();

            var userid1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var userid2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

            var user1 = new User {Id = userid1 };
            var user2 = new User { Id = userid2 };
            var dog = new Dog {Id = Guid.Parse("00000000-0000-0000-0000-000000000010") };

            using (var session = OpenSession(readingDocumentDb))
            {
                session.Save(user1);
                session.Save(user2);
                session.Save(dog);

                var ids = session.GetAllIds<User>().ToSet();

                ids.Count.Should().Be(2);
                ids.Should().Contain(userid1);
                ids.Should().Contain(userid2);
            }
        }


        [Test]
        public void DeletingAllObjectsOfATypeLeavesNoSuchObjectsInTheDbButLeavesOtherObjectsInPlaceAndReturnsTheNumberOfDeletedObjects()
        {
            var store = CreateStore();

            Dictionary<Type, Dictionary<string, string>> adict = new Dictionary<Type, Dictionary<string, string>>();

            1.Through(4).ForEach(num =>
            {
                var user = new User() { Id = Guid.NewGuid()};
                store.Add(user.Id, user, adict);
            });

            1.Through(4).ForEach(num =>
            {
                var person = new Person() { Id = Guid.NewGuid() };
                store.Add(person.Id, person, adict);
            });

            store.GetAll<User>().Should().HaveCount(4);
            store.GetAll<Person>().Should().HaveCount(8); //User inherits person

            store.RemoveAll<User>().Should().Be(4);

            store.GetAll<User>().Should().HaveCount(0);

            store.GetAll<Person>().Should().HaveCount(4);

        }

        [Test]
        public void DeletingAllObjectsOfATypeLeavesObjectOfInheritingTypes()
        {
            var store = CreateStore();

            Dictionary<Type, Dictionary<string, string>> adict = new Dictionary<Type, Dictionary<string, string>>();

            1.Through(4).ForEach(num =>
            {
                var user = new User() { Id = Guid.NewGuid() };
                store.Add(user.Id, user, adict);
            });

            1.Through(4).ForEach(num =>
            {
                var person = new Person() { Id = Guid.NewGuid() };
                store.Add(person.Id, person, adict);
            });

            store.GetAll<User>().Should().HaveCount(4);
            store.GetAll<Person>().Should().HaveCount(8); //User inherits person

            store.RemoveAll<Person>().Should().Be(4);

            store.GetAll<User>().Should().HaveCount(4);

            store.GetAll<Person>().Should().HaveCount(4);

        }

        class AlwaysNewRequestIdsHttpRequestFetcher : IHttpRequestIdFetcher
        {
            public Guid GetCurrent() => Guid.NewGuid();
        }
    }
}
