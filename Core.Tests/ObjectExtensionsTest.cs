using NUnit.Framework;
using Void.Linq;
using System.Linq;

namespace Core.Tests
{
    [TestFixture]
    public class ObjectExtensionsTest
    {
        [Test]
        public void RepeatShouldCreateSequenceOfLengthEqualToParameter()
        {
            Assert.That(12.Repeat(10).Count(), Is.EqualTo(10));
        }
    }
}