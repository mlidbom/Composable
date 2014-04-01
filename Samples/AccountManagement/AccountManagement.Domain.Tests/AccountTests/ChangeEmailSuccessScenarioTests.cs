using System.Linq;
using AccountManagement.Domain.Events;
using AccountManagement.Domain.Shared;
using AccountManagement.TestHelpers.Fixtures;
using AccountManagement.TestHelpers.Scenarios;
using Composable.KeyValueStorage.Population;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.AccountTests
{
    public class ChangeEmailSuccessScenariosTests : DomainTestBase
    {
        private Account _account;
        private readonly Email _newEmail = Email.Parse("valid.email@google.com");

        [SetUp]
        public void ChangeEmail()
        {
            _account = SingleAccountFixture.Setup(Container).Account;
            using(var transaction = Container.BeginTransactionalUnitOfWorkScope())
            {
                _account.ChangeEmail(_newEmail);
                transaction.Commit();
            }
        }

        [Test]
        public void AnIUserChangedAccountEmailEventIsRaised()
        {
            MessageSpy.ReceivedMessages
                .OfType<IUserChangedAccountEmailEvent>()
                .Should().HaveCount(1);
        }

        [Test]
        public void RaisedEventHasTheCorrectEmail()
        {
            MessageSpy.ReceivedMessages.OfType<IUserChangedAccountEmailEvent>()
                .Single()
                .Email.Should().Be(_newEmail);
        }

        [Test]
        public void AccountHasTheNewEmail()
        {
            _account.Email.Should().Be(_newEmail);
        }

        [Test]
        public void RegisteringAnAccountWithTheOldEmailIsPossible()
        {
            new RegisterAccountScenario(Container).Execute();
        }

        [Test]
        public void RegisteringAnAccountWithTheNewEmailThrowsDuplicateAccountException()
        {
            var registerAccountScenario = new RegisterAccountScenario(Container)
                                          {
                                              Email = _newEmail
                                          };
            Assert.Throws<DuplicateAccountException>(() => registerAccountScenario.Execute());
        }
    }
}
