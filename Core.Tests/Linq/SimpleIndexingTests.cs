using NUnit.Framework;
using Void.Linq;

namespace Core.Tests.Linq
{
    [TestFixture]
    public class SimpleIndexingTests
    {
        [Test]
        public void ShouldIndexCorrectly()
        {
            var indexesEqualValues = 0.Through(9);
            indexesEqualValues.ForEach((value, index) => Assert.That(indexesEqualValues.AtIndex(index), Is.EqualTo(value)));

            Assert.That(indexesEqualValues.Second(), Is.EqualTo(1));
            Assert.That(indexesEqualValues.Third(), Is.EqualTo(2));
            Assert.That(indexesEqualValues.Fourth(), Is.EqualTo(3));
            Assert.That(indexesEqualValues.Fifth(), Is.EqualTo(4));
            Assert.That(indexesEqualValues.Sixth(), Is.EqualTo(5));
            Assert.That(indexesEqualValues.Seventh(), Is.EqualTo(6));
            Assert.That(indexesEqualValues.Eighth(), Is.EqualTo(7));
            Assert.That(indexesEqualValues.Ninth(), Is.EqualTo(8));

        }
    }
}