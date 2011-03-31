#region usings

using System;
using System.Linq;
using Composable.System;
using Composable.System.Linq;
using NUnit.Framework;

#endregion

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

        [Test]
        public void CallingTransformShouldBeEquivalentToCallingItsFunctionArgumentWithItsImplicitArgument()
        {
            var i = 12;
            Func<int, int> plusone = x => x + 1;
            var expected = plusone(i);
            var actual = i.Transform(plusone);

            Assert.That(expected, Is.EqualTo(actual));
        }

        [Test]
        public void CallingDoShouldCallTheSuppliedFunctionUsingTheImpliedArgument()
        {
            const int expected = 3;
            var result = 0;
            expected.Do(me => result = me);

            Assert.That(result, Is.EqualTo(expected));
        }
    }
}