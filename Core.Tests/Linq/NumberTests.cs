using System.Linq;
using NUnit.Framework;
using Void.Linq;

namespace Core.Tests.Linq
{
    [TestFixture]
    public class NumberTests
    {
        [Test]
        public void FromShouldIteraterFromThisParam()
        {
            Assert.That(Number.From(12).First(), Is.EqualTo(12));
        }

        [Test]
        public void ThroughShouldHaveLastElementEqualToArgument()
        {
            Assert.That(1.Through(12).Last(), Is.EqualTo(12));
        }

        [Test]
        public void ThroughShouldHaveCountEqualToToMinusFromPlus1()
        {
            Assert.That(12.Through(20).Count(), Is.EqualTo(20 - 12 + 1));
        }

        [Test]
        public void StepSizeShouldIterateFromThisParam()
        {
            Assert.That(12.By(2).First(), Is.EqualTo(12));
        }


        [Test]
        public void StepSizeShouldStepByStepsize()
        {
            Assert.That(12.By(2).Second(), Is.EqualTo(14));
            Assert.That(12.By(3).Second(), Is.EqualTo(15));
            Assert.That(12.By(3).Third(), Is.EqualTo(18));
        }
    }
}