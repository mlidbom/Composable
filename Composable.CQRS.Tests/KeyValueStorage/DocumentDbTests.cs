using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Composable.DDD;
using Composable.DependencyInjection;
using Composable.Persistence.DocumentDb;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Composable.CQRS.Tests.KeyValueStorage
{
    [TestFixture]
    public abstract class DocumentDbTests
    {
        IDocumentDb CreateStore() => ServiceLocator.DocumentDb();

        protected IServiceLocator ServiceLocator { get; private set; }

        protected abstract IServiceLocator CreateServiceLocator();

        [SetUp]
        public void Setup()
        {
            ServiceLocator = CreateServiceLocator();
        }

        [TearDown]
        public void TearDownTask()
        {
            ServiceLocator.Dispose();
        }

        internal void UseInTransactionalScope([InstantHandle] Action<ITestingDocumentDbReader, ITestingDocumentDbUpdater> useSession)
        {
            ServiceLocator.ExecuteUnitOfWorkInIsolatedScope(() => useSession(ServiceLocator.DocumentDbReader(), ServiceLocator.DocumentDbUpdater()));
        }

        internal void UseInScope([InstantHandle]Action<ITestingDocumentDbReader> useSession)
        {
            ServiceLocator.ExecuteInIsolatedScope(() => useSession(ServiceLocator.DocumentDbReader()));
        }

        [Test]
        public void CanSaveAndLoadAggregate()
        {
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

            UseInTransactionalScope((reader,updater) => updater.Save(user.Id, user));

            UseInScope(reader =>
                              {
                                  var loadedUser = reader.Get<User>(user.Id);

                                  Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
                                  Assert.That(loadedUser.Email, Is.EqualTo(user.Email));
                                  Assert.That(loadedUser.Password, Is.EqualTo(user.Password));

                                  Assert.That(loadedUser.Address, Is.EqualTo(user.Address));
                              });
        }


        [Test]
        public void GetAllWithIdsReturnsAsManyResultsAsPassedIds()
        {
            var ids = 1.Through(9).Select(index => Guid.Parse($"00000000-0000-0000-0000-00000000000{index}")).ToArray();

            var users = ids.Select(id => new User() { Id = id }).ToArray();

            UseInTransactionalScope((reader, updater) => users.ForEach(user => updater.Save(user)));

            UseInScope(reader => reader.Get<User>(ids.Take(5))
                                       .Select(fetched => fetched.Id)
                                       .Should()
                                       .Equal(ids.Take(5)));
        }

        [Test] public void GetAllWithIdsThrowsNoSuchDocumentExceptionExceptionIfAnyIdIsMissing()
        {
            var ids = 1.Through(9)
                       .Select(index => Guid.Parse($"00000000-0000-0000-0000-00000000000{index}"))
                       .ToArray();

            var users = ids.Select(id => new User() {Id = id})
                           .ToArray();

            UseInTransactionalScope((reader,updater) => users.ForEach(user => updater.Save(user)));

            UseInScope(reader => Assert.Throws<NoSuchDocumentException>(
                           () => reader.Get<User>(ids.Take(5)
                                                     .Append(Guid.Parse("00000000-0000-0000-0000-000000000099"))
                                                     .ToArray())
                                       // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                                       .ToArray()));
        }


        [Test]
        public void GetAllWithIdsReturnsTheSameInstanceForAnyPreviouslyFetchedDocuments()
        {
            var ids = 1.Through(9).Select(index => Guid.Parse($"00000000-0000-0000-0000-00000000000{index}")).ToArray();

            var users = ids.Select(id => new User() { Id = id }).ToArray();

            UseInTransactionalScope((reader,updater) => users.ForEach(user => updater.Save(user)));

            UseInScope(reader =>
                             {
                                 var fetchedIndividually = ids.Select(id => reader.Get<User>(id))
                                                              .ToArray();
                                 var fetchedWithGetAll = reader.Get<User>(ids)
                                                               .ToArray();

                                 fetchedIndividually.ForEach((user, index) => Assert.That(user, Is.SameAs(fetchedWithGetAll[index])));
                             });
        }



        [Test]
        public void CanSaveAndLoadAggregateForUpdate()
        {
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

            UseInTransactionalScope((reader,updater) => updater.Save(user.Id, user));

            UseInTransactionalScope((reader, updater) =>
                                    {
                                        var loadedUser = updater.GetForUpdate<User>(user.Id);

                                        Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
                                        Assert.That(loadedUser.Email, Is.EqualTo(user.Email));
                                        Assert.That(loadedUser.Password, Is.EqualTo(user.Password));

                                        Assert.That(loadedUser.Address, Is.EqualTo(user.Address));
                                    });
        }

        [Test]
        public void CallingSaveWithAnInteraceAsTypeParameterDoesNotExplode()
        {
            IPersistentEntity<Guid> user1 = new User { Id = Guid.NewGuid(), Email = "user1" };
            IPersistentEntity<Guid> user2 = new User { Id = Guid.NewGuid(), Email = "user2" };

            UseInTransactionalScope((reader, updater) =>
                                           {
                                               updater.Save(user2);
                                               updater.Save(user1.Id, user1);
                                               reader.Get<User>(user1.Id)
                                                     .Should()
                                                     .Be(user1);
                                               reader.Get<User>(user2.Id)
                                                     .Should()
                                                     .Be(user2);
                                           });

            UseInScope(reader =>
                       {
                           reader.Get<User>(user1.Id)
                                 .Id.Should()
                                 .Be(user1.Id);
                           reader.Get<User>(user2.Id)
                                 .Id.Should()
                                 .Be(user2.Id);
                       });
        }

        [Test]
        public void AddingAndRemovingObjectResultsInNoObjectBeingSaved()
        {
            var user = new User { Id = Guid.NewGuid() };

            UseInTransactionalScope((reader,updater) =>
                                           {
                                               updater.Save(user.Id, user);
                                               updater.Delete(user);
                                           });

            UseInScope(reader =>
                           reader.TryGet(user.Id, out user)
                                 .Should()
                                 .BeFalse());
        }

        [Test]
        public void AddingRemovingAndAddingObjectInUnitOfWorkResultsInNoObjectBeingSaved()
        {
            var user = new User { Id = Guid.NewGuid() };

            UseInTransactionalScope((reader,updater) =>
                                           {
                                               updater.Save(user.Id, user);
                                               updater.Delete(user);
                                               updater.Save(user.Id, user);
                                           });

            UseInScope(reader => reader.TryGet(user.Id, out user).Should().BeTrue());
        }

        [Test]
        public void ObjectsWhoseKeysDifferOnlyByCaseAreConsideredTheSameObjectForCompatabilityWithSqlServer()
        {
            var lowerCase = new Email("theemail");
            var upperCase = new Email(lowerCase.TheEmail.ToUpper());

            UseInTransactionalScope((reader, updater) =>
                                           {
                                               updater.Save(lowerCase.TheEmail, lowerCase);
                                               Assert.Throws<AttemptToSaveAlreadyPersistedValueException>(() => updater.Save(upperCase.TheEmail, upperCase));

                                               reader.Get<Email>(lowerCase.TheEmail)
                                                     .Should()
                                                     .Be(reader.Get<Email>(upperCase.TheEmail));
                                           });

            UseInTransactionalScope((reader, updater) =>
                                    {

                                        Assert.Throws<AttemptToSaveAlreadyPersistedValueException>(() => updater.Save(upperCase.TheEmail, upperCase));
                                        reader.Get<Email>(upperCase.TheEmail)
                                              .TheEmail.Should()
                                              .Be(lowerCase.TheEmail);
                                        reader.Get<Email>(lowerCase.TheEmail)
                                              .Should()
                                              .Be(reader.Get<Email>(upperCase.TheEmail));

                                        updater.Delete<Email>(upperCase.TheEmail);
                                        Assert.Throws<NoSuchDocumentException>(() => updater.Delete<Email>(upperCase.TheEmail));
                                        Assert.Throws<NoSuchDocumentException>(() => updater.Delete<Email>(lowerCase.TheEmail));
                                    });
        }

        [Test]
        public void ObjectsWhoseKeysDifferOnlyByTrailingSpacesTrailingWhiteSpaceCaseAreConsideredTheSameObjectForCompatabilityWithSqlServer()
        {
            var noWhitespace = new Email("theemail");
            var withWhitespace = new Email(noWhitespace.TheEmail + "  ");

            UseInTransactionalScope((reader, updater) =>
            {
                    updater.Save(noWhitespace.TheEmail, noWhitespace);
                    Assert.Throws<AttemptToSaveAlreadyPersistedValueException>(() => updater.Save(withWhitespace.TheEmail, withWhitespace));

                    reader.Get<Email>(noWhitespace.TheEmail)
                           .Should()
                           .Be(reader.Get<Email>(withWhitespace.TheEmail));
                });

            UseInTransactionalScope((reader, updater) =>
            {
                    Assert.Throws<AttemptToSaveAlreadyPersistedValueException>(() => updater.Save(withWhitespace.TheEmail, withWhitespace));
                    reader.Get<Email>(withWhitespace.TheEmail)
                           .TheEmail.Should()
                           .Be(noWhitespace.TheEmail);
                    reader.Get<Email>(noWhitespace.TheEmail)
                           .Should()
                           .Be(reader.Get<Email>(withWhitespace.TheEmail));

                    updater.Delete<Email>(withWhitespace.TheEmail);
                    Assert.Throws<NoSuchDocumentException>(() => updater.Delete<Email>(withWhitespace.TheEmail));
                    Assert.Throws<NoSuchDocumentException>(() => updater.Delete<Email>(noWhitespace.TheEmail));
                });
        }

        [Test]
        public void TryingToFetchNonExistentItemDoesNotCauseSessionToTryAndAddItWithANullInstance()
        {
            var user = new User { Id = Guid.NewGuid() };

            UseInScope(reader => reader.TryGet(user.Id, out user)
                                       .Should()
                                       .Be(false));
        }

        [Test]
        public void RepeatedlyAddingAndRemovingObjectResultsInNoObjectBeingSaved()
        {
            var user = new User { Id = Guid.NewGuid() };

            UseInTransactionalScope((reader, updater) =>
                                    {
                                        updater.Save(user.Id, user);
                                        updater.Delete(user);
                                        updater.Save(user.Id, user);
                                        updater.Delete(user);
                                        updater.Save(user.Id, user);
                                        updater.Delete(user);
                                    });

            UseInScope(reader => reader.TryGet(user.Id, out user)
                                       .Should()
                                       .BeFalse());
        }

        [Test]
        public void LoadingRemovingAndAddingObjectInUnitOfWorkResultsInObjectBeingSaved()
        {
            var user = new User { Id = Guid.NewGuid() };

            UseInTransactionalScope((reader, updater) => updater.Save(user.Id, user));

            UseInTransactionalScope((reader, updater) =>
                                    {
                                        user = reader.Get<User>(user.Id);
                                        updater.Delete(user);

                                        User tmpUser;
                                        reader.TryGet(user.Id, out tmpUser)
                                              .Should()
                                              .Be(false);
                                        updater.Save(user);
                                        reader.TryGet(user.Id, out tmpUser)
                                              .Should()
                                              .Be(true);
                                        updater.Delete(user);
                                        reader.TryGet(user.Id, out tmpUser)
                                              .Should()
                                              .Be(false);
                                        updater.Save(user);
                                        reader.TryGet(user.Id, out tmpUser)
                                              .Should()
                                              .Be(true);
                                    });

            UseInScope(reader => reader.TryGet(user.Id, out user)
                                         .Should()
                                         .Be(true));
        }


        [Test]
        public void ReturnsSameInstanceOnRepeatedLoads()
        {
            var user = new User { Id = Guid.NewGuid() };

            UseInTransactionalScope((reader, updater) => updater.Save(user.Id, user));

            UseInScope(reader =>
                       {
                           var loaded1 = reader.Get<User>(user.Id);
                           var loaded2 = reader.Get<User>(user.Id);
                           Assert.That(loaded1, Is.SameAs(loaded2));
                       });
        }

        [Test]
        public void ReturnsSameInstanceOnLoadAfterSave()
        {
            var user = new User { Id = Guid.NewGuid() };

            UseInTransactionalScope((reader, updater) =>
                                    {
                                        updater.Save(user.Id, user);

                                        var loaded1 = reader.Get<User>(user.Id);
                                        var loaded2 = reader.Get<User>(user.Id);
                                        Assert.That(loaded1, Is.SameAs(loaded2));
                                        Assert.That(loaded1, Is.SameAs(user));
                                    });
        }

        [Test]
        public void HandlesHashSets()
        {
            var user = new User { Id = Guid.NewGuid() };
            var userSet = new HashSet<User> { user };

            UseInTransactionalScope((reader, updater) => updater.Save(user.Id, userSet));

            UseInScope(reader =>
                       {
                           var loadedUser = reader.Get<HashSet<User>>(user.Id);
                           Assert.That(loadedUser.Count, Is.EqualTo(1));
                       });
        }

        [Test]
        public void HandlesHashSetsInObjects()
        {
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

            UseInTransactionalScope((reader, updater) => updater.Save(user.Id, user));

            UseInScope(reader =>
                       {
                           var loadedUser = reader.Get<User>(user.Id);
                           Assert.That(loadedUser.People.Count, Is.EqualTo(1));
                           var loadedUserInSet = loadedUser.People.Single();
                           Assert.That(loadedUserInSet.Id, Is.EqualTo(userInSet.Id));
                       });
        }


        [Test]
        public void ThrowsExceptionWhenAttemptingToDeleteNonExistingValue()
        {
            UseInTransactionalScope((reader, updater) =>
                                    {
                                        var lassie = new Dog {Id = Guid.NewGuid()};
                                        updater.Save(lassie);
                                    });

            var buster = new Dog { Id = Guid.NewGuid() };
            UseInTransactionalScope((reader, updater) => Assert.Throws<NoSuchDocumentException>(() => updater.Delete(buster)));
        }

        [Test]
        public void HandlesDeletesOfInstancesAlreadyLoaded()
        {
            var user = new User { Id = Guid.NewGuid() };

            UseInTransactionalScope((reader, updater) => updater.Save(user));

            UseInTransactionalScope((reader, updater) =>
                                    {
                                        var loadedUser = updater.GetForUpdate<User>(user.Id);
                                        loadedUser.Should()
                                                  .NotBeNull();
                                        updater.Delete(user);

                                        Assert.Throws<NoSuchDocumentException>(() => reader.Get<User>(user.Id));
                                    });

            UseInScope(reader => Assert.Throws<NoSuchDocumentException>(() => reader.Get<User>(user.Id)));
        }

        [Test]
        public void HandlesDeletesOfInstancesNotYetLoaded()
        {
            var user = new User { Id = Guid.NewGuid() };

            UseInTransactionalScope((reader, updater) => updater.Save(user));

            UseInTransactionalScope((reader, updater) =>
                                    {
                                        updater.Delete(user);
                                        Assert.Throws<NoSuchDocumentException>(() => updater.GetForUpdate<User>(user.Id));
                                    });

            UseInScope(reader => Assert.Throws<NoSuchDocumentException>(() => reader.Get<User>(user.Id)));
        }

        [Test]
        public void HandlesAValueBeingAddedAndDeletedDuringTheSameSession()
        {
            var user = new User { Id = Guid.NewGuid() };

            UseInTransactionalScope((reader, updater) =>
                                    {
                                        updater.Save(user);
                                        updater.Delete(user);
                                        Assert.Throws<NoSuchDocumentException>(() => updater.GetForUpdate<User>(user.Id));
                                    });

            UseInScope(reader => Assert.Throws<NoSuchDocumentException>(() => reader.Get<User>(user.Id)));
        }

        [Test]
        public void TracksAndUpdatesLoadedAggregates()
        {
            var user = new User { Id = Guid.NewGuid() };

            UseInTransactionalScope((reader, updater) => updater.Save(user.Id, user));

            UseInTransactionalScope((reader, updater) =>
                                    {
                                        var loadedUser = updater.GetForUpdate<User>(user.Id);
                                        loadedUser.Password = "NewPassword";
                                    });

            UseInScope(reader =>
            {
                var loadedUser = reader.Get<User>(user.Id);
                Assert.That(loadedUser.Password, Is.EqualTo("NewPassword"));
            });
        }

        [Test]
        public void ThrowsWhenAttemptingToSaveExistingAggregate()
        {
            var user = new User { Id = Guid.NewGuid() };

            UseInTransactionalScope((reader, updater) => updater.Save(user.Id, user));

            Assert.Throws<AttemptToSaveAlreadyPersistedValueException>(() => UseInTransactionalScope((reader, updater) => updater.Save(user.Id, user)));
        }

        [Test]
        public void HandlesInstancesOfDifferentTypesWithTheSameId()
        {
            var user = new User
                       {
                           Id = Guid.NewGuid(),
                           Email = "email"
                       };

            var dog = new Dog { Id = user.Id };

            UseInTransactionalScope((reader, updater) =>
                                    {
                                        updater.Save<IPersistentEntity<Guid>>(user);
                                        updater.Save<IPersistentEntity<Guid>>(dog);
                                    });

            UseInScope(reader =>
                       {
                           var loadedDog = reader.Get<Dog>(dog.Id);
                           var loadedUser = reader.Get<User>(dog.Id);

                           Assert.That(loadedDog.Name, Is.EqualTo(dog.Name));
                           Assert.That(loadedUser.Email, Is.EqualTo(user.Email));
                           Assert.That(loadedDog.Id, Is.EqualTo(user.Id));
                           Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
                       });
        }


        [Test]
        public void FetchesAllinstancesPerType()
        {
            UseInTransactionalScope((reader, updater) =>
                                    {
                                        updater.Save(new User {Id = Guid.NewGuid()});
                                        updater.Save(new User {Id = Guid.NewGuid()});
                                        updater.Save(new Dog {Id = Guid.NewGuid()});
                                        updater.Save(new Dog {Id = Guid.NewGuid()});
                                    });

            using (ServiceLocator.BeginScope())
            {
                Assert.That(ServiceLocator.DocumentDbBulkReader().GetAll<Dog>().ToList(), Has.Count.EqualTo(2));
                Assert.That(ServiceLocator.DocumentDbBulkReader().GetAll<User>().ToList(), Has.Count.EqualTo(2));
            }
        }

        [Test]
        public void ThrowsIfUsedByMultipleThreads()
        {
            IDocumentDbSession session = null;
            var wait = new ManualResetEventSlim();
            ThreadPool.QueueUserWorkItem(state =>
                                         {
                                             ServiceLocator.ExecuteInIsolatedScope(() => session = ServiceLocator.DocumentDbSession());
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
            Assert.Throws<MultiThreadedUseException>(() => session.TryGet(Guid.NewGuid(), out user));
            Assert.Throws<MultiThreadedUseException>(() => session.TryGetForUpdate(user.Id, out user));
            Assert.Throws<MultiThreadedUseException>(() => session.Delete(user));
        }


        [Test]
        public void GetHandlesSubTyping()
        {
            var user1 = new User { Id = Guid.NewGuid() };
            var person1 = new Person { Id = Guid.NewGuid() };

            UseInTransactionalScope((reader, updater) =>
                                    {
                                        updater.Save(user1);
                                        updater.Save(person1);
                                    });

            UseInScope(reader =>
                       {
                           Assert.That(reader.Get<Person>(user1.Id), Is.EqualTo(user1));
                           Assert.That(reader.Get<Person>(person1.Id), Is.EqualTo(person1));
                       });
        }

        [Test]
        public void GetAllHandlesSubTyping()
        {
            var user1 = new User { Id = Guid.NewGuid() };
            var person1 = new Person { Id = Guid.NewGuid() };

            UseInTransactionalScope((reader, updater) =>
                                    {
                                        updater.Save(user1);
                                        updater.Save(person1);
                                    });

            using (ServiceLocator.BeginScope())
            {
                var people = ServiceLocator.DocumentDbBulkReader().GetAll<Person>().ToList();

                Assert.That(people, Has.Count.EqualTo(2));
                Assert.That(people, Contains.Item(user1));
                Assert.That(people, Contains.Item(person1));
            }
        }

        [Test]
        public void ThrowsExceptionIfYouTryToSaveAnIHasPersistentIdentityWithNoId()
        {
            var user1 = new User { Id = Guid.Empty };

            UseInTransactionalScope((reader, updater) => updater.Invoking(@this => @this.Save(user1))
                                                                .ShouldThrow<Exception>());
        }

        [Test]
        public void GetByIdsShouldReturnOnlyMatchingResultEvenWhenMoreResultsAreInTheCache()
        {
            var user1 = new User { Id = Guid.Parse("00000000-0000-0000-0000-000000000001") };
            var user2 = new User { Id = Guid.Parse("00000000-0000-0000-0000-000000000002") };

            UseInTransactionalScope((reader, updater) =>
                                    {
                                        updater.Save(user1);
                                        updater.Save(user2);

                                        var people = reader.Get<User>(new[] {user1.Id});

                                        Assert.That(people.ToList(), Has.Count.EqualTo(1));
                                        Assert.That(people, Contains.Item(user1));
                                    });
        }


        [Test]
        public void GetAllIdsShouldOnlyReturnResultsWithTheGivenType()
        {
            var userid1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var userid2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

            var user1 = new User {Id = userid1 };
            var user2 = new User { Id = userid2 };
            var dog = new Dog {Id = Guid.Parse("00000000-0000-0000-0000-000000000010") };

            UseInTransactionalScope((reader, updater) =>
                                    {
                                        updater.Save(user1);
                                        updater.Save(user2);
                                        updater.Save(dog);
                                    });

            ServiceLocator.ExecuteInIsolatedScope(() =>
                                                  {
                                                      var ids = ServiceLocator.DocumentDbBulkReader()
                                                                              .GetAllIds<User>()
                                                                              .ToSet();

                                                      ids.Count.Should()
                                                         .Be(2);
                                                      ids.Should()
                                                         .Contain(userid1);
                                                      ids.Should()
                                                         .Contain(userid2);
                                                  });
        }

        [Test, Ignore("Bug found while refactoring. Fix soon but ignore for now")]
        public void GetAllIdsShouldOnlyReturnResultsWithTheGivenTypeWhenCalledWithinTheInsertingTransaction()
        {
            var userid1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var userid2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

            var user1 = new User { Id = userid1 };
            var user2 = new User { Id = userid2 };
            var dog = new Dog { Id = Guid.Parse("00000000-0000-0000-0000-000000000010") };

            UseInTransactionalScope((reader, updater) =>
                                    {
                                        updater.Save(user1);
                                        updater.Save(user2);
                                        updater.Save(dog);

                                        var ids = ServiceLocator.DocumentDbBulkReader()
                                                                .GetAllIds<User>()
                                                                .ToSet();

                                        ids.Count.Should()
                                           .Be(2);
                                        ids.Should()
                                           .Contain(userid1);
                                        ids.Should()
                                           .Contain(userid2);
                                    });
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
    }
}
