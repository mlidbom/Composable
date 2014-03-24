using AccountManagement.Domain.Shared;
using AccountManagement.TestHelpers.Fixtures;
using Composable.Contracts;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.AccountTests
{
    [TestFixture]
    public class ChangePasswordFailureScenariosTests : DomainTestBase
    {
        private ValidAccountRegisteredFixture _fixture;
        private Account _account;

        [SetUp]
        public void RegisterAccount()
        {
            _fixture = new ValidAccountRegisteredFixture();
            _account = _fixture.Setup(Container);
        }

        [Test]
        public void WhenNewPasswordIsNullObjectNullExceptionIsThrown()
        {
            Assert.Throws<ObjectIsNullException>(() => _account.ChangePassword(_fixture.PasswordAsString, null));
        }

        [Test]
        public void WhenOldPasswordIsIncorrectWrongPasswordExceptionIsThrown()
        {
            Assert.Throws<WrongPasswordException>(() => _account.ChangePassword("wrong", new Password("SomeComplexPassword1!")));
        }
    }
}