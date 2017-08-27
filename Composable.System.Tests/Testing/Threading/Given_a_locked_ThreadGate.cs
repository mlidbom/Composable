using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Threading;
using System.Threading.Tasks;
using Composable.Testing.Threading;
using FluentAssertions;

namespace Composable.Tests.Testing.Threading
{
    [TestFixture()]
    public class Given_a_locked_ThreadGate
    {
        AutoResetEvent _autoResetEventBeforeGate;
        [SetUp] public void SetupTask()
        {
            _autoResetEventBeforeGate = new AutoResetEvent(initialState:false);
        }

        [Test] public void Code_after_the_gate_is_not_executed_until_the_gate_is_opened()
        {
            Assert.Inconclusive();
        }
    }


    [TestFixture] public class Given_a_TestingAutoResetEvent
    {
        TestingAutoResetEvent _sut = new TestingAutoResetEvent("MyTestEvent", startLocked: true, timeout: 5.Seconds());

        [SetUp] public void That_is_locked_()
        {
            _sut.Set();
        }

        [Test] public void Expectation()
        {
            
            Assert.Inconclusive();
        }

        [TestFixture] public class Scenario
        {
            [Test] public void Expectation()
            {
                
                Assert.Inconclusive();
            }
        }
    }
}
