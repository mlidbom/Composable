using System.Linq;
using System.Threading.Tasks;
using AccountManagement.API;
using AccountManagement.Domain.Events;
using AccountManagement.Tests.Scenarios;
using Composable.Messaging.Buses;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Tests.Domain.After_a_user_has_registered_an_account
{
    [TestFixture] public class Then_ : DomainTestBase
    {
        AccountResource _registeredAccount;
        RegisterAccountScenario _registerAccountScenario;

        [SetUp] public void RegisterAccount()
        {
            _registerAccountScenario = new RegisterAccountScenario(ClientBus);
            _registeredAccount = _registerAccountScenario.Execute();
        }

        [Test] public void An_IUserRegisteredAccountEvent_is_published() => MessageSpy.DispatchedMessages.OfType<AccountEvent.UserRegistered>().ToList().Should().HaveCount(1);

        [Test] public void AccountEmail_is_the_one_used_for_registration() => Assert.That(_registeredAccount.Email.ToString(), Is.EqualTo(_registerAccountScenario.Command.Email));

        [Test] public void AccountPassword_is_the_one_used_for_registration() => Assert.True(_registeredAccount.Password.IsCorrectPassword(_registerAccountScenario.Command.Password));

        [Test] public async Task Login_with_the_correct_email_and_password_succeeds_returning_non_null_and_nonEmpty_authenticationToken()
        {
            var result = await ClientBus.Get(AccountApi.Start)
                                        .Post(start => start.Commands.Login.New(_registerAccountScenario.Command.Email, _registerAccountScenario.Command.Password))
                                        .ExecuteNavigationAsync();

            result.Succeeded.Should().Be(true);
            result.AuthenticationToken.Should().NotBe(null).And.NotBe(string.Empty);
        }

        [Test] public async Task Login_with_the_correct_email_but_wrong_password_fails()
        {
            (await ClientBus.Get(AccountApi.Start)
                            .Post(start => start.Commands.Login.New(_registerAccountScenario.Command.Email, "SomeOtherPassword"))
                            .ExecuteNavigationAsync())
                .Succeeded.Should().Be(false);
        }

        [Test] public async Task Login_with_the_wrong_email_but_correct_password_fails()
        {
            (await ClientBus.Get(AccountApi.Start)
                            .Post(start => start.Commands.Login.New("some_other@email.com", _registerAccountScenario.Command.Password))
                            .ExecuteNavigationAsync())
                .Succeeded.Should().Be(false);
        }
    }
}
