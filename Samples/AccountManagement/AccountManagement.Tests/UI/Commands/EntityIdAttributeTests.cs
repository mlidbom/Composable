using System;
using AccountManagement.UI.Commands.Tests.UserCommands;
using AccountManagement.UI.Commands.ValidationAttributes;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;

namespace AccountManagement.UI.Commands.Tests
{
    [TestFixture]
    public class EntityIdAttributeTests
    {
        [Test]
        public void IsValidIfIdIsNull()
        {
            CommandValidator.ValidationFailures(new ACommand() {AnId = null})
                .Should().BeEmpty();
        }

        [Test]
        public void IsNotValidIfIdIsEmpty()
        {
            CommandValidator.ValidationFailures(new ACommand() {AnId = Guid.Empty})
                .Should().NotBeEmpty();
        }

        class ACommand
        {
            [EntityId]
            public Guid? AnId { [UsedImplicitly] get; set; }
        }
    }
}
