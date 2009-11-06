using NUnit.Framework;
using Void.Linq;
using System.Linq;

namespace Core.Tests.Linq
{
    [TestFixture]
    public class ZippingTests
    {
        [Test]
        public void ShouldIncludeAllElementsFromSequencesOfEqualLength()
        {
            var oneToTen = 1.To(10);
            var elevenToTwenty = 11.To(20);

            var zipped = oneToTen.Zip(elevenToTwenty);

            var oneToTenResult = zipped.Select(pair => pair.First);
            var elevenToTwentyResult = zipped.Select(pair => pair.Second);
            Assert.That(oneToTen, Is.EquivalentTo(oneToTenResult));
            Assert.That(elevenToTwenty, Is.EquivalentTo(elevenToTwentyResult));
        }

        [Test]
        public void ShouldIncludeAllElementsFromTheShorterSequence()
        {
            var oneToFive = 1.To(5);
            var sixToEight = 6.To(8);

            var zipped = oneToFive.Zip(sixToEight);

            var oneToFiveResult = zipped.Select(pair => pair.First);
            var sixToEightResult = zipped.Select(pair => pair.Second);
            Assert.That(oneToFive.Take(3), Is.EquivalentTo(oneToFiveResult));
            Assert.That(sixToEightResult, Is.EquivalentTo(sixToEightResult));
        }
    }
}