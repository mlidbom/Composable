using System.Threading.Tasks;
using AccountManagement.Tests.Scenarios;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Tests.Domain.After_a_user_has_registered_an_account
{
    [TestFixture] public class Then_Logging_in : DomainTestBase
    {
        RegisterAccountScenario _registerAccountScenario;

        [SetUp] public void RegisterAccount()
        {
            _registerAccountScenario = new RegisterAccountScenario(ClientBus);
            _registerAccountScenario.Execute();
        }

        [Test] public async Task with_the_correct_email_and_password_succeeds_returning_non_null_and_nonEmpty_authenticationToken()
        {
            var result = await new LoginScenario(ClientBus, _registerAccountScenario.Email, _registerAccountScenario.Password).ExecuteAsync();

            result.Succeeded.Should().Be(true);
            result.AuthenticationToken.Should().NotBe(null).And.NotBe(string.Empty);
        }

        [Test] public async Task with_the_correct_email_but_wrong_password_fails()
            => (await new LoginScenario(ClientBus, _registerAccountScenario.Email, "SomeOtherPassword").ExecuteAsync())
               .Succeeded.Should().Be(false);

        [Test] public async Task with_the_wrong_email_but_correct_password_fails()
            => (await new LoginScenario(ClientBus, "some_other@email.com", _registerAccountScenario.Password).ExecuteAsync())
               .Succeeded.Should().Be(false);
    }
}
