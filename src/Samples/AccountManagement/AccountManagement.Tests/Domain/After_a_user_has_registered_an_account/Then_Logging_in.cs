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
            _registerAccountScenario = new RegisterAccountScenario(DomainEndpoint);
            _registerAccountScenario.Execute();
        }

        [Test] public void with_the_correct_email_and_password_succeeds_returning_non_null_and_nonEmpty_authenticationToken()
        {
            var result = new LoginScenario(DomainEndpoint, _registerAccountScenario.Email, _registerAccountScenario.Password).Execute();

            result.Succeeded.Should().Be(true);
            result.AuthenticationToken.Should().NotBe(null).And.NotBe(string.Empty);
        }

        [Test] public void with_the_correct_email_but_wrong_password_fails()
            => new LoginScenario(DomainEndpoint, _registerAccountScenario.Email, "SomeOtherPassword").Execute()
               .Succeeded.Should().Be(false);

        [Test] public void with_the_wrong_email_but_correct_password_fails()
            => new LoginScenario(DomainEndpoint, "some_other@email.com", _registerAccountScenario.Password).Execute()
               .Succeeded.Should().Be(false);
    }
}
