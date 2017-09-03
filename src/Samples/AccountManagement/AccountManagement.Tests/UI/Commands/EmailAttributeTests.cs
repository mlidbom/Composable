using AccountManagement.Tests.UI.Commands.UserCommands;
using AccountManagement.UI.Commands.ValidationAttributes;
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
            CommandValidator.ValidationFailures(new ACommand() {Email = "aoeustnh"})
                .Should().NotBeEmpty();
        }

        class ACommand
        {
            [Email]
            public string Email { [UsedImplicitly] get; set; }
        }
    }
}
