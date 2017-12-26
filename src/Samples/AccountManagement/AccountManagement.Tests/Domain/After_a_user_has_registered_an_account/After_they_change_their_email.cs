using System.Linq;
using System.Threading.Tasks;
using AccountManagement.Domain;
using AccountManagement.Domain.Events;
using AccountManagement.Tests.Scenarios;
using Composable.Messaging.Buses;
using Composable.System.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Tests.Domain.After_a_user_has_registered_an_account
{
    public class After_they_change_their_email : DomainTestBase
    {
        ChangeAccountEmailScenario _changeEmailScenario;

        [SetUp]
        public async Task ChangeEmail()
        {
            _changeEmailScenario = await ChangeAccountEmailScenario.CreateAsync(ClientBus);
            _changeEmailScenario.Execute();
        }

        [Test]
        public void an_IUserChangedAccountEmailEvent_is_published_on_the_bus()
        {
            MessageSpy.DispatchedMessages
                .OfType<AccountEvent.UserChangedEmail>()
                .Should().HaveCount(1);
        }

        [Test]
        public void Raised_event_contains_the_supplied_email()
        {
            MessageSpy.DispatchedMessages
                .OfType<AccountEvent.UserChangedEmail>().Single()
                .Email.Should().Be(_changeEmailScenario.NewEmail);
        }

        [Test]
        public void Account_Email_is_the_supplied_email()
        {
            _changeEmailScenario.Account.Email.Should().Be(_changeEmailScenario.NewEmail);
        }

        [Test]
        public async Task Registering_an_account_with_the_old_email_works()
        {
            await new RegisterAccountScenario(ClientBus,email: _changeEmailScenario.OldEmail.ToString()).ExecuteAsync();
        }

        [Test]
        public void Attempting_to_register_an_account_with_the_new_email_throws_a_DuplicateAccountException()
        {
            var scenario = new RegisterAccountScenario(ClientBus, email: _changeEmailScenario.NewEmail.ToString());

            Host.AssertThatRunningScenarioThrowsBackendException<DuplicateAccountException>(() => scenario.ExecuteAsync().Wait())
                .Message.Should().Contain(_changeEmailScenario.Account.Email.ToString());
        }
    }
}
