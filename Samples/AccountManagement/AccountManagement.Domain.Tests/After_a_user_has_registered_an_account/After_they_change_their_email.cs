using System.Linq;
using AccountManagement.Domain.Events;
using AccountManagement.TestHelpers.Scenarios;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.After_a_user_has_registered_an_account
{
    public class After_they_change_their_email : DomainTestBase
    {
        ChangeAccountEmailScenario _changeEmailScenario;

        [SetUp]
        public void ChangeEmail()
        {
            _changeEmailScenario = new ChangeAccountEmailScenario(ServiceLocator);
            _changeEmailScenario.Execute();
        }

        [Test]
        public void an_IUserChangedAccountEmailEvent_is_published_on_the_bus()
        {
            MessageSpy.DispatchedMessages
                .OfType<IUserChangedAccountEmailEvent>()
                .Should().HaveCount(1);
        }

        [Test]
        public void Raised_event_contains_the_supplied_email()
        {
            MessageSpy.DispatchedMessages
                .OfType<IUserChangedAccountEmailEvent>().Single()
                .Email.Should().Be(_changeEmailScenario.NewEmail);
        }

        [Test]
        public void Account_Email_is_the_supplied_email()
        {
            _changeEmailScenario.Account.Email.Should().Be(_changeEmailScenario.NewEmail);
        }

        [Test]
        public void Registering_an_account_with_the_old_email_works()
        {
            new RegisterAccountScenario(ServiceLocator)
            {
                Email = _changeEmailScenario.OldEmail
            }.Execute();
        }

        [Test]
        public void Attempting_to_register_an_account_with_the_new_email_throws_a_DuplicateAccountException()
        {
            new RegisterAccountScenario(ServiceLocator)
                {
                    Email = _changeEmailScenario.NewEmail
                }.Invoking(me => me.Execute())
                .ShouldThrow<DuplicateAccountException>();
        }
    }
}
