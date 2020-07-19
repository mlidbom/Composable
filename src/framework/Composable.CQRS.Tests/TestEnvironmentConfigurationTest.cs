using NUnit.Framework;

namespace Composable.Tests
{
    [TestFixture]public class TestEnvironmentConfigurationTest
    {
        //Todo: Verify that the environment setting seems sane. Also try splitting the environment variable into at least three. [IO,THREADED,SINGLETHREADED]_PERFORMANCE_FACTOR
        [Test, Ignore("todo")] public void Composable_performance_environment_variable_has_sane_value()
        {
            Assert.Inconclusive();
        }
    }
}
