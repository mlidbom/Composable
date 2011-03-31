#region usings

using System;
using System.Collections.Generic;
using Composable.Data.ORM.Repositories.Tests.Domain;
using NUnit.Framework;

#endregion

namespace Composable.Data.ORM.Repositories.Tests
{
    public abstract class RepositoryTest
    {
        protected abstract IPersistenceSession GetPersistanceSession();

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