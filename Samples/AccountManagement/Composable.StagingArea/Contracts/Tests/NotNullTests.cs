using FluentAssertions;
using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    [TestFixture]
    public class NotNullTests
    {
        [Test]
        public void UsesArgumentNameForExceptionmessage()
        {
            Assert.Throws<ObjectIsNullException>(() => Contract.Argument<string>(null, "argument").NotNull())
                .Message.Should().Contain("argument");
        }
    }
}