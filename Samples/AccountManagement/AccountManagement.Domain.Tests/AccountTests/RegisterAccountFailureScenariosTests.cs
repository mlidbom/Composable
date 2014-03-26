using System;
using AccountManagement.Domain.Services;
using AccountManagement.TestHelpers.Scenarios;
using Composable.Contracts;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.AccountTests
{
    [TestFixture]
    public class RegisterAccountFailureScenariosTests : DomainTestBase
    {
        private IAccountManagementEventStoreSession _repository;
        private IDuplicateAccountChecker _duplicateAccountChecker;
        private RegisterAccountScenario _registerAccountScenario;

        [SetUp]
        public void SetupWiringAndCreateRepositoryAndScope()
        {
            _repository = Container.Resolve<IAccountManagementEventStoreSession>();
            _duplicateAccountChecker = Container.Resolve<IDuplicateAccountChecker>();
            _registerAccountScenario = new RegisterAccountScenario(Container);
        }

        [Test]
        public void WhenEmailIsAlreadyRegisteredADuplicateAccountExceptionIsThrown()
        {
            _registerAccountScenario.Execute();
            Assert.Throws<DuplicateAccountException>(() => _registerAccountScenario.Execute());
        }

        [Test]
        public void WhenPasswordIsNullObjectIsNullExceptionIsThrown()
        {
            _registerAccountScenario.Password = null;
            Assert.Throws<ObjectIsNullContractViolationException>(() => _registerAccountScenario.Execute());
        }

        [Test]
        public void WhenEmailIsNullObjectIsNullExceptionIsThrown()
        {
            _registerAccountScenario.Email = null;
            Assert.Throws<ObjectIsNullContractViolationException>(() => _registerAccountScenario.Execute());
        }

        [Test]
        public void WhenAccountIdIsEmptyObjectIsDefaultExceptionIsThrown()
        {
            _registerAccountScenario.AccountId = Guid.Empty;
            Assert.Throws<ObjectIsDefaultContractViolationException>(() => _registerAccountScenario.Execute());
        }

        [Test]
        public void WhenRepositoryIsNullObjectIsNullExceptionIsThrown()
        {
            Assert.Throws<ObjectIsDefaultContractViolationException>(
                () => Account.Register(_registerAccountScenario.Email, _registerAccountScenario.Password, Guid.Empty, _repository, _duplicateAccountChecker));
        }
    }
}
