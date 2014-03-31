using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AccountManagement.TestHelpers;
using AccountManagement.UI.Commands.UserCommands;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UI.Commands.Tests.UserCommands
{
    [TestFixture]
    public class RegisterAccountCommandTests
    {
        private RegisterAccountCommand _registerAccountCommand;

        [SetUp]
        public void CreateValidCommand()
        {
            _registerAccountCommand = new RegisterAccountCommand()
                                      {
                                          AccountId = Guid.NewGuid(),
                                          Email = "valid.email@google.com",
                                          Password = "AComplex!1Password"
                                      };
            Validate(_registerAccountCommand).Should().BeEmpty();
        }

        [Test]
        public void IsInvalidifAccountIdIsEmpty()
        {
            _registerAccountCommand.AccountId = Guid.Empty;
            Validate(_registerAccountCommand).Should().NotBeEmpty();
        }

        [Test]
        public void IsInvalidIfEmailIsNull()
        {
            _registerAccountCommand.Email = null;
            Validate(_registerAccountCommand).Should().NotBeEmpty();
        }

        [Test]
        public void IsInvalidIfEmailIsIncorrectFormat()
        {
            _registerAccountCommand.Email = "invalid";
            Validate(_registerAccountCommand).Should().NotBeEmpty();
        }

        [Test]
        public void IsInvalidIfPasswordIsNull()
        {
            _registerAccountCommand.Password = null;
            Validate(_registerAccountCommand).Should().NotBeEmpty();
        }

        [Test]
        public void IsInvalidIfPasswordDoesNotMatchPolicy()
        {
            foreach(var invalidPassword in TestData.Password.Invalid.All)
            {
                _registerAccountCommand.Password = invalidPassword;
                Validate(_registerAccountCommand).Should().NotBeEmpty();   
            }            
        }

        [Test]
        public void WhenNotMatchingThePolicyTheFailureTellsHow()
        {
            _registerAccountCommand.Password = TestData.Password.Invalid.ShorterThanFourCharacters;
            ValidateAndGetFirstMessage().Should().Be(RegisterAccountCommandResources.Password_ShorterThanFourCharacters);

            _registerAccountCommand.Password = TestData.Password.Invalid.BorderedByWhiteSpaceAtEnd;
            ValidateAndGetFirstMessage().Should().Be(RegisterAccountCommandResources.Password_BorderedByWhitespace);

            _registerAccountCommand.Password = TestData.Password.Invalid.MissingLowercaseCharacter;
            ValidateAndGetFirstMessage().Should().Be(RegisterAccountCommandResources.Password_MissingLowerCaseCharacter);

            _registerAccountCommand.Password = TestData.Password.Invalid.MissingUpperCaseCharacter;
            ValidateAndGetFirstMessage().Should().Be(RegisterAccountCommandResources.Password_MissingUpperCaseCharacter);
        }

        [Test]
        public void FailsIfUnHandledPolicyFailureIsDetected()
        {
            _registerAccountCommand.Password = null; //Null is normally caught by the Require attribute.
            _registerAccountCommand.Invoking(command => command.Validate(null).ToArray()).ShouldThrow<Exception>();
        }

        private string ValidateAndGetFirstMessage()
        {
            return Validate(_registerAccountCommand).First().ErrorMessage;
        }

        private IEnumerable<ValidationResult> Validate(object command)
        {
            var context = new ValidationContext(command, serviceProvider: null, items: null);
            var results = new List<ValidationResult>();

            Validator.TryValidateObject(command, context, results, validateAllProperties: true);
            return results;
        }
    }
}
