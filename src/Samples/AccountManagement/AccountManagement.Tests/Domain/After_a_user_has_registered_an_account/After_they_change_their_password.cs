using AccountManagement.Tests.Scenarios;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Tests.Domain.After_a_user_has_registered_an_account
{
    public class After_they_change_their_password : DomainTestBase
    {
        ChangePasswordScenario _changePasswordScenario;

        [SetUp] public void RegisterAccount()
        {
            _changePasswordScenario = ChangePasswordScenario.Create(ClientEndpoint);
            _changePasswordScenario.Execute();
        }

        [Test] public void Logging_in_with_the_new_password_works() =>
            new LoginScenario(ClientEndpoint, _changePasswordScenario.Account, _changePasswordScenario.NewPasswordAsString).Execute().Succeeded.Should().Be(true);

        [Test] public void Logging_in_with_the_old_password_fails() =>
            new LoginScenario(ClientEndpoint, _changePasswordScenario.Account, _changePasswordScenario.OldPassword).Execute().Succeeded.Should().Be(false);
    }
}
