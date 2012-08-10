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
                Assert.Throws<Exception>(() => new failing_spec().ValidateSpec());
            }
    }

    [Ignore]

    public class failing_spec : NSpec.NUnit.nspec
    {
        public void strange_math()
        {
            it["1 equals 2"] = () => 1.should_be(2);
        }
    }
}