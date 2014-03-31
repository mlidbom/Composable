using System;
using AccountManagement.UI.Commands.Tests.UserCommands;
using AccountManagement.UI.Commands.ValidationAttributes;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UI.Commands.Tests
{
    [TestFixture]
    public class EntityIdAttributeTests
    {
        [Test]
        public void IsValidIfIdIsNull()
        {
            CommandValidator.Validate(new ACommand() {AnId = null})
                .Should().BeEmpty();
        }

        [Test]
        public void IsNotValidIfIdIsEmpty()
        {
            CommandValidator.Validate(new ACommand() { AnId = Guid.Empty })
                .Should().NotBeEmpty();
        }

        private class ACommand
        {
            [EntityId]
            public Guid? AnId { get; set; }
        }
    }
}