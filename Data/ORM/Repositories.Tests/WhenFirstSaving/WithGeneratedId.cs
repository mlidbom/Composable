using System;
using NUnit.Framework;
using Composable.Data.ORM.Repositories.Tests.Domain;

namespace Composable.Data.ORM.Repositories.Tests.WhenFirstSaving
{
    [TestFixture]
    public class WithGeneratedId : RepositoryTest
    {
        [Test]
        public void ShouldAssignIdWhenSavingObjectWithDefaultId()
        {
            var instance = GetInstance();
            Repository.SaveOrUpdate(instance);
            Assert.That(instance.Id, Is.Not.EqualTo(0), "instance.Id");
        }
    }
}