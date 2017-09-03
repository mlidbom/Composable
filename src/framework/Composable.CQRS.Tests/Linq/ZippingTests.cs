using System.Linq;
using Composable.System.Linq;
using NUnit.Framework;

namespace Composable.Tests.Linq
{
    [TestFixture]
    public class ZippingTests
    {
        [Test]
        public void ShouldIncludeAllElementsFromSequencesOfEqualLength()
        {
            var oneToTen = 1.Through(10);
            var elevenToTwenty = 11.Through(20);

            var zipped = oneToTen.Zip(elevenToTwenty);

            var oneToTenResult = zipped.Select(pair => pair.First);
            var elevenToTwentyResult = zipped.Select(pair => pair.Second);
            Assert.That(oneToTen, Is.EquivalentTo(oneToTenResult));
            Assert.That(elevenToTwenty, Is.EquivalentTo(elevenToTwentyResult));
        }

        [Test]
        public void ShouldIncludeAllElementsFromTheShorterSequence()
        {
            var oneToFive = 1.Through(5);
            var sixToEight = 6.Through(8);

            var zipped = oneToFive.Zip(sixToEight);

            var oneToFiveResult = zipped.Select(pair => pair.First);
            var sixToEightResult = zipped.Select(pair => pair.Second);
            Assert.That(oneToFive.Take(3), Is.EquivalentTo(oneToFiveResult));
            Assert.That(sixToEightResult, Is.EquivalentTo(sixToEightResult));
        }
    }
}