using AccountManagement.API.ValidationAttributes;
using AccountManagement.Tests.UI.Commands.UserCommands;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;

namespace AccountManagement.Tests.UI.Commands
{
    [TestFixture]
    public class EmailAttributeTests
    {
        [Test]
        public void IsValidIfEmailIsNull()
        {
            CommandValidator.ValidationFailures(new ACommand() {Email = null})
                .Should().BeEmpty();
        }

        [Test]
        public void IsValidIfEmailIsEmpty()
        {
            CommandValidator.ValidationFailures(new ACommand() {Email = ""})
                .Should().BeEmpty();
        }

        [Test]
        public void IsNotValidIfEmailIsInvalid()
        {
            CommandValidator.ValidationFailures(new ACommand() {Email = "InvalidEmail"})
                .Should().NotBeEmpty();
        }

        class ACommand
        {
            [Email]
            public string Email { [UsedImplicitly] get; set; }
        }
    }
}
