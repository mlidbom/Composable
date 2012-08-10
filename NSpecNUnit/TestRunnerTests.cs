using System;
using NSpec;
using NUnit.Framework;

namespace NSpecNUnit
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
    public class failing_spec : NSpecTestBase
    {
        public void strange_math()
        {
            it["1 equals 2"] = () => 1.should_be(2);
        }
    }
}