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

        [SetUp]
        public void Setup()
        {
            Session = GetPersistanceSession();
        }

        [TearDown]
        public void TearDown()
        {
            Session.Dispose();
        }
    }
}