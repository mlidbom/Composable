#region usings

using System;
using System.Collections.Generic;
using Composable.Data.ORM.InMemoryRepositories;
using Composable.Data.ORM.Repositories.Tests.Domain;
using NUnit.Framework;

#endregion

namespace Composable.Data.ORM.Repositories.Tests
{
    public class RepositoryTest
    {
        protected virtual IPersistenceSession GetPersistanceSession()
        {
            IDictionary<Type, IIdManager> idManagers = new Dictionary<Type, IIdManager>
                                                           {
                                                               {
                                                                   typeof(TypeWithGeneratedId), new Int32IdManager<TypeWithGeneratedId>
                                                                                                    {
                                                                                                        Getter = me => me.Id,
                                                                                                        Setter =
                                                                                                            (me, value) => me.Id = value
                                                                                                    }
                                                                   }
                                                           };
            return new HashSetPersistanceSession(idManagers);
        }

        protected virtual TypeWithGeneratedId GetInstance()
        {
            return new TypeWithGeneratedId();
        }

        protected IPersistenceSession Session { get; set; }
        protected IRepository<TypeWithGeneratedId, Int32> Repository { get; set; }

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
    }
}