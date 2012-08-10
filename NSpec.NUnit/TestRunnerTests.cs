using System;
using NUnit.Framework;

// ReSharper disable InconsistentNaming
namespace NSpec.NUnit
{
    [TestFixture]
    public class TestRunnerTests
    {
            [Test]
            public void throws_on_failing_spec()
            {
                Assert.Throws<SpecificationException>(() => new specification_of_xfiles_math().ValidateSpec());
            }
    }

    [Ignore]
    public class specification_of_xfiles_math : NSpec.NUnit.nspec
    {
        public void when_using_strange_math()
        {
            it["1 equals 2"] = () => 1.should_be(2);
        }
    }
}