using System.Linq;
using Composable.SystemCE.LinqCE;
using NUnit.Framework;

namespace Composable.Tests
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