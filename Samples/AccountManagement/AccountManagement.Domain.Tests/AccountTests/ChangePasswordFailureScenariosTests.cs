using AccountManagement.Domain.Shared;
using AccountManagement.TestHelpers.Fixtures;
using Composable.Contracts;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.AccountTests
{
    [TestFixture]
    public class ChangePasswordFailureScenariosTests : DomainTestBase
    {
        private AccountRegisteredFixture _registeredAccountFixture;
        private Account _account;

        [SetUp]
        public void RegisterAccount()
        {
            _registeredAccountFixture = new AccountRegisteredFixture();
            _account = _registeredAccountFixture.Setup(Container);
        }

        [Test]
        public void WhenNewPasswordIsNullObjectNullExceptionIsThrown()
        {
            Assert.Throws<ObjectIsNullException>(() => _account.ChangePassword(_registeredAccountFixture.PasswordAsString, null));
        }

        [Test]
        public void WhenOldPasswordIsNullObjectNullExceptionIsThrown()
        {
            Assert.Throws<ObjectIsNullException>(() => _account.ChangePassword(null, new Password("SomeComplexPassword1!")));
        }

        [Test]
        public void WhenOldPasswordIsEmptyStringIsEmptyExceptionIsThrown()
        {
            Assert.Throws<StringIsEmptyException>(() => _account.ChangePassword("", new Password("SomeComplexPassword1!")));
        }

        [Test]
        public void WhenOldPasswordIsIncorrectWrongPasswordExceptionIsThrown()
        {
            Assert.Throws<WrongPasswordException>(() => _account.ChangePassword("wrong", new Password("SomeComplexPassword1!")));
        }
    }
}