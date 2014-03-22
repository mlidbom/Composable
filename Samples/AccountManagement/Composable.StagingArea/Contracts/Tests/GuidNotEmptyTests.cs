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
            Assert.Throws<GuidIsEmptyException>(() => Contract.ArgumentsOptimized(Guid.Empty).NotEmpty());
            Assert.Throws<GuidIsEmptyException>(() => Contract.ArgumentOptimized(Guid.Empty, "aGuid").NotEmpty())
                .Message.Should().Contain("aGuid");
        }
    }
}
