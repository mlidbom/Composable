using System;
using System.Collections.Generic;
using NUnit.Framework;
using Void.Data.ORM.InMemoryTesting;
using Void.Data.ORM.Repositories.Tests.Domain;

namespace Void.Data.ORM.Repositories.Tests.WhenFirstSavingInstance
{
    [TestFixture]
    public class WithGeneratedId
    {
        private IPersistanceSession Session { get; set; }
        private IRepository<TypeWithGeneratedId, Int32> Repository { get; set; }

        [SetUp]
        public void Setup()
        {
            Session = GetPersistanceSession();
            Repository = new Repository<TypeWithGeneratedId, Int32>(Session);
        }

        [TearDown]
        public void TearDown()
        {
            Session.Dispose();
        }

        [Test]
        public void ShouldAssignIdWhenSavingObjectWithDefaultId()
        {
            var instance = GetInstance();
            Repository.SaveOrUpdate(instance);
            Assert.That(instance.Id, Is.Not.EqualTo(0), "instance.Id");
        }

        protected virtual IPersistanceSession GetPersistanceSession()
        {
            IDictionary<Type, IIdManager> idManagers = new Dictionary<Type, IIdManager>
                                                           {
                                                               {
                                                                   typeof (TypeWithGeneratedId), new Int32IdManager<TypeWithGeneratedId>
                                                                                                     {
                                                                                                         Getter = me => me.Id,
                                                                                                         Setter = (me, value) => me.Id = value
                                                                                                     }
                                                                   }
                                                           };
            return new HashSetPersistanceSession(idManagers);
        }

        protected virtual TypeWithGeneratedId GetInstance()
        {
            return new TypeWithGeneratedId();
        }
    }
}