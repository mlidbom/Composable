using System.Linq;
using AccountManagement.Domain.Events;
using AccountManagement.TestHelpers.Scenarios;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.AccountTests
{
    public class ChangeEmailSuccessScenariosTests : DomainTestBase
    {
        ChangeAccountEmailScenario _changeEmailScenario;

        [SetUp]
        public void ChangeEmail()
        {
            _changeEmailScenario = new ChangeAccountEmailScenario(Container);
            _changeEmailScenario.Execute();
        }

        [Test]
        public void ASingleIUserChangedAccountEmailEventIsRaised()
        {
            MessageSpy.ReceivedMessages
                .OfType<IUserChangedAccountEmailEvent>()
                .Should().HaveCount(1);
        }

        [Test]
        public void RaisedEventHasTheCorrectEmail()
        {
            MessageSpy.ReceivedMessages
                .OfType<IUserChangedAccountEmailEvent>().Single()
                .Email.Should().Be(_changeEmailScenario.NewEmail);
        }

        [Test]
        public void AccountHasTheNewEmail()
        {
            _changeEmailScenario.Account.Email.Should().Be(_changeEmailScenario.NewEmail);
        }

        [Test]
        public void RegisteringAnAccountWithTheOldEmailIsPossible()
        {
            new RegisterAccountScenario(Container)
            {
                Email = _changeEmailScenario.OldEmail
            }.Execute();
        }

        [Test]
        public void RegisteringAnAccountWithTheNewEmailThrowsDuplicateAccountException()
        {
            new RegisterAccountScenario(Container)
                {
                    Email = _changeEmailScenario.NewEmail
                }.Invoking(me => me.Execute())
                .ShouldThrow<DuplicateAccountException>();
        }
    }
}
