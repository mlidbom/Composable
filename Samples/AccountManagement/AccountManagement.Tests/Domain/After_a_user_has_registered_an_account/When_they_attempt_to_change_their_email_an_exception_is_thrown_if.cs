using System;
using AccountManagement.TestHelpers.Scenarios;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.After_a_user_has_registered_an_account
{
    public class When_they_attempt_to_change_their_email_an_exception_is_thrown_if : DomainTestBase
    {
        ChangeAccountEmailScenario _changeEmail;

        [SetUp]
        public void RegisterAccount()
        {
            _changeEmail = new ChangeAccountEmailScenario(ServiceLocator);
        }

        [Test]
        public void NewEmail_is_null()
        {
            _changeEmail.NewEmail = null;
            _changeEmail.Invoking(scenario => scenario.Execute())
                .ShouldThrow<Exception>();
        }
    }
}
