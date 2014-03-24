using System;
using AccountManagement.Domain.Services;
using AccountManagement.TestHelpers;
using AccountManagement.TestHelpers.Fixtures;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using Composable.Contracts;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.AccountTests
{
    [TestFixture]
    public class RegisterAccountFailureScenariosTests   
    {
        private IDisposable _scope;
        private IAccountManagementEventStoreSession _repository;
        private WindsorContainer _container;
        private IDuplicateAccountChecker _duplicateAccountChecker;
        private ValidAccountRegisteredFixture _validAccountRegisteredFixture;

        [SetUp]
        public void SetupWiringAndCreateRepositoryAndScope()
        {
            _container = DomainTestWiringHelper.SetupContainerForTesting();
            _scope = _container.BeginScope();
            _repository = _container.Resolve<IAccountManagementEventStoreSession>();
            _duplicateAccountChecker = _container.Resolve<IDuplicateAccountChecker>();
            _validAccountRegisteredFixture = new ValidAccountRegisteredFixture();
            //_fixture.Execute(_container);
        }

        [TearDown]
        public void CleanupScope()
        {
            _scope.Dispose();
        }

        [Test]
        public void WhenEmailIsAlreadyRegisteredADuplicateAccountExceptionIsThrown()
        {
            _validAccountRegisteredFixture.Setup(_container);
            Assert.Throws<DuplicateAccountException>(() => Account.Register(_validAccountRegisteredFixture.Email, _validAccountRegisteredFixture.Password, _validAccountRegisteredFixture.AccountId, _repository, _duplicateAccountChecker));
        }

        [Test]
        public void WhenPasswordIsNullObjectIsNullExceptionIsThrown()
        {
            Assert.Throws<ObjectIsNullException>(() => Account.Register(_validAccountRegisteredFixture.Email, null, _validAccountRegisteredFixture.AccountId, _repository, _duplicateAccountChecker));
        }

        [Test]
        public void WhenEmailIsNullObjectIsNullExceptionIsThrown()
        {
            Assert.Throws<ObjectIsNullException>(() => Account.Register(null, _validAccountRegisteredFixture.Password, _validAccountRegisteredFixture.AccountId, _repository, _duplicateAccountChecker));
        }

        [Test]
        public void WhenAccountIdIsEmptyObjectIsDefaultExceptionIsThrown()
        {
            Assert.Throws<ObjectIsDefaultException>(() => Account.Register(_validAccountRegisteredFixture.Email, _validAccountRegisteredFixture.Password, Guid.Empty, _repository, _duplicateAccountChecker));
        }

        [Test]
        public void WhenRepositoryIsNullObjectIsNullExceptionIsThrown()
        {
            Assert.Throws<ObjectIsDefaultException>(() => Account.Register(_validAccountRegisteredFixture.Email, _validAccountRegisteredFixture.Password, Guid.Empty, _repository, _duplicateAccountChecker));
        }
    }
}
