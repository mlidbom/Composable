using AccountManagement.Domain.Shared;
using AccountManagement.TestHelpers.Scenarios;
using Composable.Contracts;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.AccountTests
{
    [TestFixture]
    public class ChangePasswordFailureScenariosTests : DomainTestBase
    {
        private RegisterAccountScenario _registerAccountScenario;
        private Account _account;

        [SetUp]
        public void RegisterAccount()
        {
            _registerAccountScenario = new RegisterAccountScenario(Container);
            _account = _registerAccountScenario.Execute();
        }

        [Test]
        public void WhenNewPasswordIsNullObjectNullExceptionIsThrown()
        {
            Assert.Throws<ObjectIsNullContractViolationException>(() => _account.ChangePassword(_registerAccountScenario.PasswordAsString, null));
        }

        [Test]
        public void WhenOldPasswordIsNullObjectNullExceptionIsThrown()
        {
            Assert.Throws<ObjectIsNullContractViolationException>(() => _account.ChangePassword(null, new Password("SomeComplexPassword1!")));
        }

        [Test]
        public void WhenOldPasswordIsEmptyStringIsEmptyExceptionIsThrown()
        {
            Assert.Throws<StringIsEmptyContractViolationException>(() => _account.ChangePassword("", new Password("SomeComplexPassword1!")));
        }

        [Test]
        public void WhenOldPasswordIsIncorrectWrongPasswordExceptionIsThrown()
        {
            Assert.Throws<WrongPasswordException>(() => _account.ChangePassword("wrong", new Password("SomeComplexPassword1!")));
        }
    }
}
