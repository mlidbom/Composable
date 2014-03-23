using System;
using AccountManagement.Domain.Services;
using AccountManagement.Domain.Shared;
using Castle.MicroKernel.Lifestyle;
using Composable.Contracts;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.AccountTests
{
    [TestFixture]
    public class RegisterAccountFailureTests
    {
        private IDisposable _scope;
        private IAccountManagementEventStoreSession _repository;
        private readonly Password _validPassword = new Password("Password1");
        private readonly Email _validEmail = Email.Parse("test.test@test.se");
        private readonly Guid _validAccountId = Guid.NewGuid();

        [SetUp]
        public void SetupWiringAndCreateRepositoryAndScope()
        {
            var container = DomainTestWiringHelper.SetupContainerForTesting();
            _scope = container.BeginScope();
            _repository = container.Resolve<IAccountManagementEventStoreSession>();
        }

        [TearDown]
        public void CleanupScope()
        {
            _scope.Dispose();
        }

        [Test]
        public void WhenPasswordIsNullObjectIsNullExceptionIsThrown()
        {
            Assert.Throws<ObjectIsNullException>(() => Account.Register(null, _validPassword, _validAccountId, _repository));
        }

        [Test]
        public void WhenEmailIsNullObjectIsNullExceptionIsThrown()
        {
            Assert.Throws<ObjectIsNullException>(() => Account.Register(_validEmail, null, _validAccountId, _repository));
        }

        [Test]
        public void WhenAccountIdIsEmptyObjectIsDefaultExceptionIsThrown()
        {
            Assert.Throws<ObjectIsDefaultException>(() => Account.Register(_validEmail, _validPassword, Guid.Empty, _repository));
        }

        [Test]
        public void WhenRepositoryIsNullObjectIsNullExceptionIsThrown()
        {
            Assert.Throws<ObjectIsDefaultException>(() => Account.Register(_validEmail, _validPassword, Guid.Empty, _repository));
        }
    }
}
