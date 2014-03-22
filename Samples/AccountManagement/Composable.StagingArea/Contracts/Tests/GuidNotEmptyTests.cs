using System;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    [TestFixture]
    public class GuidNotEmptyTests
    {
        [Test]
        public void NotEmptyThrowsArgumentExceptionForEmptyGuid()
        {
            Assert.Throws<GuidIsEmptyException>(() => Contract.Arguments(Guid.Empty).NotEmpty());
            Assert.Throws<GuidIsEmptyException>(() => Contract.Argument(Guid.Empty, "aGuid").NotEmpty())
                .Message.Should().Contain("aGuid");
        }
    }
}
