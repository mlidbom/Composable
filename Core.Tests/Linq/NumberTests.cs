using System.Linq;
using System.Threading;
using NUnit.Framework;
using Void.Linq;

namespace Core.Tests.Linq
{
    [TestFixture]
    public class NumberTests
    {
        [Test]
        public void ThroughShouldHaveLastElementEqualToArgumentMinusStepSizeWhenSteppingByOneOreMinusOne()
        {
            Assert.That(1.Until(12).Last(), Is.EqualTo(12 - 1));
            Assert.That(1.By(1).Until(12).Last(), Is.EqualTo(12 - 1));
            Assert.That((-1).By(-1).Until(-12).Last(), Is.EqualTo(-12 - (-1)));
        }

        [Test]
        public void ThroughShouldHaveLastElementEqualToArgument()
        {
            Assert.That(1.Through(12).Last(), Is.EqualTo(12));
            Assert.That(1.By(1).Through(12).Last(), Is.EqualTo(12));
            Assert.That((-1).By(-1).Through(-12).Last(), Is.EqualTo(-12));
        }

        [Test]
        public void ThroughShouldHaveCountEqualToToMinusFromPlus1()
        {
            Assert.That(12.Through(20).Count(), Is.EqualTo(20 - 12 + 1));
            Assert.That(12.By(1).Through(20).Count(), Is.EqualTo(20 - 12 + 1));
            Assert.That((-12).By(-1).Through(-20).Count(), Is.EqualTo(20 - 12 + 1));
        }

        [Test]
        public void StepSizeShouldIterateFromImplicitParameter()
        {
            Assert.That(12.By(2).Through(int.MaxValue).First(), Is.EqualTo(12));
            Assert.That((-12).By(-2).Through(-int.MaxValue).First(), Is.EqualTo(-12));
        }


        [Test]
        public void StepSizeShouldStepByStepsize()
        {
            Assert.That(12.By(2).Through(int.MaxValue).Second(), Is.EqualTo(14));
            Assert.That(12.By(3).Through(int.MaxValue).Second(), Is.EqualTo(15));
            Assert.That(12.By(3).Through(int.MaxValue).Third(), Is.EqualTo(18));

            Assert.That((-12).By(-2).Through(-int.MaxValue).Second(), Is.EqualTo(-14));
            Assert.That((-12).By(-3).Through(-int.MaxValue).Second(), Is.EqualTo(-15));
            Assert.That((-12).By(-3).Through(-int.MaxValue).Third(), Is.EqualTo(-18));
        }
    }
}