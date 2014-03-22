using FluentAssertions;
using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    [TestFixture]
    public class ObjectNotNullTests
    {
        [Test]
        public void UsesArgumentNameForExceptionmessage()
        {
            Assert.Throws<ObjectIsNullException>(() => Contract.ArgumentOptimized<string>(null, "argument").NotNull())
                .Message.Should().Contain("argument");
        }
    }
}
