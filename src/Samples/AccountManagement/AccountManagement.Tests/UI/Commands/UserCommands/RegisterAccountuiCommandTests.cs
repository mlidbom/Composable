using System;
using System.Linq;
using AccountManagement.API;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Tests.UI.Commands.UserCommands
{
    [TestFixture]
    public class RegisterAccountUICommandTests
    {
        AccountResource.Command.Register.UICommand _registerAccountUiCommand;

        [SetUp]
        public void CreateValidCommand()
        {
            _registerAccountUiCommand = new AccountResource.Command.Register.UICommand()
                                      {
                                          Email = "valid.email@google.com",
                                          Password = "AComplex!1Password"
                                      };
            CommandValidator.ValidationFailures(_registerAccountUiCommand).Should().BeEmpty();
        }

        [Test]
        public void IsInvalidifAccountIdIsEmpty()
        {
            _registerAccountUiCommand.AccountId = Guid.Empty;
            CommandValidator.ValidationFailures(_registerAccountUiCommand).Should().NotBeEmpty();
        }

        [Test]
        public void IsInvalidIfEmailIsNull()
        {
            _registerAccountUiCommand.Email = null;
            CommandValidator.ValidationFailures(_registerAccountUiCommand).Should().NotBeEmpty();
        }

        [Test]
        public void IsInvalidIfEmailIsIncorrectFormat()
        {
            _registerAccountUiCommand.Email = "invalid";
            CommandValidator.ValidationFailures(_registerAccountUiCommand).Should().NotBeEmpty();
        }

        [Test]
        public void IsInvalidIfPasswordIsNull()
        {
            _registerAccountUiCommand.Password = null;
            CommandValidator.ValidationFailures(_registerAccountUiCommand).Should().NotBeEmpty();
        }

        [Test]
        public void IsInvalidIfPasswordDoesNotMatchPolicy()
        {
            foreach(var invalidPassword in TestData.Password.Invalid.All)
            {
                _registerAccountUiCommand.Password = invalidPassword;
                CommandValidator.ValidationFailures(_registerAccountUiCommand).Should().NotBeEmpty();
            }
        }

        [Test]
        public void WhenNotMatchingThePolicyTheFailureTellsHow()
        {
            _registerAccountUiCommand.Password = TestData.Password.Invalid.ShorterThanFourCharacters;
            ValidateAndGetFirstMessage().Should().Be(RegisterAccountCommandResources.Password_ShorterThanFourCharacters);

            _registerAccountUiCommand.Password = TestData.Password.Invalid.BorderedByWhiteSpaceAtEnd;
            ValidateAndGetFirstMessage().Should().Be(RegisterAccountCommandResources.Password_BorderedByWhitespace);

            _registerAccountUiCommand.Password = TestData.Password.Invalid.MissingLowercaseCharacter;
            ValidateAndGetFirstMessage().Should().Be(RegisterAccountCommandResources.Password_MissingLowerCaseCharacter);

            _registerAccountUiCommand.Password = TestData.Password.Invalid.MissingUpperCaseCharacter;
            ValidateAndGetFirstMessage().Should().Be(RegisterAccountCommandResources.Password_MissingUpperCaseCharacter);

            _registerAccountUiCommand.Password = TestData.Password.Invalid.Null;
            ValidateAndGetFirstMessage().Should().Be(RegisterAccountCommandResources.PasswordMissing);
        }

        [Test]
        public void FailsIfUnHandledPolicyFailureIsDetected()
        {
            _registerAccountUiCommand.Password = null; //Null is normally caught by the Require attribute.
            // ReSharper disable once AssignNullToNotNullAttribute
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            _registerAccountUiCommand.Invoking(command => command.Validate(null).ToArray()).ShouldThrow<Exception>();
        }

        string ValidateAndGetFirstMessage() => CommandValidator.ValidationFailures(_registerAccountUiCommand).First().ErrorMessage;
    }
}
