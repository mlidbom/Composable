using System;
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
            new EntityIdAttribute().IsValid(null).Should().Be(true);
        }

        [Test]
        public void IsNotValidIfIdIsEmpty()
        {
            new EntityIdAttribute().IsValid(Guid.Empty).Should().Be(false);
        }
    }
}