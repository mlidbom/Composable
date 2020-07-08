using System;
using Composable.System;
using Composable.System.Threading;
using Composable.Testing;
using Composable.Testing.Performance;
using FluentAssertions;
using NCrunch.Framework;
using NUnit.Framework;

namespace Composable.Tests.System.Threading
{
    [TestFixture, Performance, Serial] public class PersistentMachineWideSharedObjectPerformanceTests
    {
        MachineWideSharedObject<SharedObject> _shared;
        [SetUp] public void SetupTask()
        {
            var name = Guid.NewGuid().ToString();
            _shared = MachineWideSharedObject<SharedObject>.For(name, usePersistentFile: true);
        }

        [TearDown] public void TearDownTask() => _shared.Dispose();

        [Test] public void Get_copy_runs_single_threaded_100_times_in_6_milliseconds()
         => TimeAsserter.Execute(() => _shared.GetCopy(), iterations: 100, maxTotal: 6.Milliseconds());

        [Test] public void Get_copy_runs_multi_threaded_100_times_in_10_milliseconds() =>
            TimeAsserter.ExecuteThreaded(() => _shared.GetCopy(), iterations: 100, maxTotal: 10.Milliseconds());

        [Test] public void Update_runs_single_threaded_100_times_in_17_milliseconds() =>
            TimeAsserter.Execute(() => _shared.Update(@this => @this.Name = ""), iterations: 100, maxTotal: 17.Milliseconds(), maxTries: 10);

        [Test] public void Update_runs_multi_threaded_100_times_in_25_milliseconds() =>
            TimeAsserter.ExecuteThreaded(() => _shared.Update(@this => @this.Name = ""), iterations: 100, maxTotal: 25.Milliseconds(), maxTries: 10);
    }
}
