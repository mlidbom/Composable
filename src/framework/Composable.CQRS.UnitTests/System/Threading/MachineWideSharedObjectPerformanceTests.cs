using System;
using Composable.SystemCE;
using Composable.SystemCE.Reflection.Threading;
using Composable.Testing;
using Composable.Testing.Performance;
using FluentAssertions;
using NCrunch.Framework;
using NUnit.Framework;

namespace Composable.Tests.System.Threading
{
    [TestFixture, Performance, Serial] public class MachineWideSharedObjectPerformanceTests
    {
        [Test] public void Get_copy_runs_single_threaded_1000_times_in_60_milliseconds()
        {
            var name = Guid.NewGuid().ToString();
            using var shared = MachineWideSharedObject<SharedObject>.For(name);
            using var shared2 = MachineWideSharedObject<SharedObject>.For(name);
            TimeAsserter.Execute(() => shared.GetCopy(), iterations: 1000, maxTotal: 60.Milliseconds());
            TimeAsserter.Execute(() => shared2.GetCopy(), iterations: 1000, maxTotal: 60.Milliseconds());
        }

        [Test] public void Get_copy_runs_multi_threaded_1000_times_in_60_milliseconds()
        {
            var name = Guid.NewGuid().ToString();
            using var shared = MachineWideSharedObject<SharedObject>.For(name);
            using var shared2 = MachineWideSharedObject<SharedObject>.For(name);
            TimeAsserter.ExecuteThreaded(() => shared.GetCopy(), iterations: 1000, maxTotal: 60.Milliseconds());
            TimeAsserter.ExecuteThreaded(() => shared2.GetCopy(), iterations: 1000, maxTotal: 60.Milliseconds());
        }

        [Test] public void Update_runs_single_threaded_1000_times_in_80_milliseconds()
        {
            MachineWideSharedObject<SharedObject> shared = null!;
            var counter = 0;

            TimeAsserter.Execute(
                setup: () =>
                {
                    counter = 0;
                    shared = MachineWideSharedObject<SharedObject>.For(Guid.NewGuid().ToString());
                },
                tearDown: () =>
                {
                    shared.GetCopy().Name.Should().Be("1000");
                    shared.Dispose();
                },
                action: () => shared.Update(@this => @this.Name = (++counter).ToString()),
                iterations: 1000,
                maxTotal: 80.Milliseconds());
        }

        [Test] public void Update_runs_multi_threaded_1000_times_in_120_milliseconds()
        {
            MachineWideSharedObject<SharedObject> shared = null!;
            var counter = 0;

            TimeAsserter.ExecuteThreaded(
                setup: () =>
                {
                    counter = 0;
                    shared = MachineWideSharedObject<SharedObject>.For(Guid.NewGuid().ToString());
                },
                tearDown: () =>
                {
                    shared.GetCopy().Name.Should().Be("1000");
                    shared.Dispose();
                },
                action: () => shared.Update(@this => @this.Name = (++counter).ToString()),
                iterations: 1000,
                maxTotal: 120.Milliseconds());
        }
    }
}
