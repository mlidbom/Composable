using System;
using AccountManagement.Domain.Services;
using AccountManagement.TestHelpers.Fixtures;
using Composable.Contracts;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.AccountTests
{
    [TestFixture]
    public class RegisterAccountFailureScenariosTests : DomainTestBase
    {
        private IAccountManagementEventStoreSession _repository;
        private IDuplicateAccountChecker _duplicateAccountChecker;
        private ValidAccountRegisteredFixture _accountFixture;

        [SetUp]
        public void SetupWiringAndCreateRepositoryAndScope()
        {
            _repository = Container.Resolve<IAccountManagementEventStoreSession>();
            _duplicateAccountChecker = Container.Resolve<IDuplicateAccountChecker>();
            _accountFixture = new ValidAccountRegisteredFixture();
        }

        [Test]
        public void WhenEmailIsAlreadyRegisteredADuplicateAccountExceptionIsThrown()
        {
            _accountFixture.Setup(Container);
            Assert.Throws<DuplicateAccountException>(() => Account.Register(_accountFixture.Email, _accountFixture.Password, _accountFixture.AccountId, _repository, _duplicateAccountChecker));
        }

        [Test]
        public void WhenPasswordIsNullObjectIsNullExceptionIsThrown()
        {
            Assert.Throws<ObjectIsNullException>(() => Account.Register(_accountFixture.Email, null, _accountFixture.AccountId, _repository, _duplicateAccountChecker));
        }

        [Test]
        public void WhenEmailIsNullObjectIsNullExceptionIsThrown()
        {
            Assert.Throws<ObjectIsNullException>(() => Account.Register(null, _accountFixture.Password, _accountFixture.AccountId, _repository, _duplicateAccountChecker));
        }

        [Test]
        public void WhenAccountIdIsEmptyObjectIsDefaultExceptionIsThrown()
        {
            Assert.Throws<ObjectIsDefaultException>(() => Account.Register(_accountFixture.Email, _accountFixture.Password, Guid.Empty, _repository, _duplicateAccountChecker));
        }

        [Test]
        public void WhenRepositoryIsNullObjectIsNullExceptionIsThrown()
        {
            Assert.Throws<ObjectIsDefaultException>(() => Account.Register(_accountFixture.Email, _accountFixture.Password, Guid.Empty, _repository, _duplicateAccountChecker));
        }
    }
}
