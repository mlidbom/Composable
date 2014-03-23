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
            string nullString = null;
            Assert.Throws<ObjectIsNullException>(() => Contract.Optimized.Argument(nullString, "nullString").NotNull())
                .Message.Should().Contain("nullString");

            Assert.Throws<ObjectIsNullException>(() => Contract.Arguments(() => nullString).NotNull())
                .Message.Should().Contain("nullString");
        }
    }
}
