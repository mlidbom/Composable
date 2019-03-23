using System;
using Composable.System;
using Composable.System.Threading;
using Composable.Testing.Performance;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.System.Threading
{
    [TestFixture] public class MachineWideSharedObjectPerformanceTests
    {
        [Test] public void Get_copy_runs_single_threaded_1000_times_in_60_milliseconds()
        {
            var name = Guid.NewGuid().ToString();
            using(var shared = MachineWideSharedObject<SharedObject>.For(name))
            using(var shared2 = MachineWideSharedObject<SharedObject>.For(name))
            {
                TimeAsserter.Execute(() => shared.GetCopy(), iterations: 1000, maxTotal: 60.Milliseconds());
                TimeAsserter.Execute(() => shared2.GetCopy(), iterations: 1000, maxTotal: 60.Milliseconds());
            }
        }

        [Test] public void Get_copy_runs_multi_threaded_1000_times_in_60_milliseconds()
        {
            var name = Guid.NewGuid().ToString();
            using(var shared = MachineWideSharedObject<SharedObject>.For(name))
            using(var shared2 = MachineWideSharedObject<SharedObject>.For(name))
            {
                TimeAsserter.ExecuteThreaded(() => shared.GetCopy(), iterations: 1000, maxTotal: 60.Milliseconds());
                TimeAsserter.ExecuteThreaded(() => shared2.GetCopy(), iterations: 1000, maxTotal: 60.Milliseconds());
            }
        }

        [Test] public void Update_runs_single_threaded_1000_times_in_80_milliseconds()
        {
            var name = Guid.NewGuid().ToString();
            using(var shared = MachineWideSharedObject<SharedObject>.For(name))
            using(var shared2 = MachineWideSharedObject<SharedObject>.For(name))
            {
                int counter = 0;
                TimeAsserter.Execute(() => shared.Update(@this => @this.Name = (++counter).ToString()), iterations: 1000, maxTotal: 80.Milliseconds(), maxTries: 1);
                shared.GetCopy().Name.Should().Be("1000");
                TimeAsserter.Execute(() => shared2.Update(@this => @this.Name = (++counter).ToString()), iterations: 1000, maxTotal: 80.Milliseconds(), maxTries: 1);
                shared2.GetCopy().Name.Should().Be("2000");
            }
        }

        [Test] public void Update_runs_multi_threaded_1000_times_in_120_milliseconds()
        {
            var name = Guid.NewGuid().ToString();
            using(var shared = MachineWideSharedObject<SharedObject>.For(name))
            using(var shared2 = MachineWideSharedObject<SharedObject>.For(name))
            {
                int counter = 0;
                TimeAsserter.ExecuteThreaded(() => shared.Update(@this => @this.Name = (++counter).ToString()), iterations: 1000, maxTotal: 120.Milliseconds(), maxTries: 1);
                shared.GetCopy().Name.Should().Be("1000");
                TimeAsserter.ExecuteThreaded(() => shared2.Update(@this => @this.Name = (++counter).ToString()), iterations: 1000, maxTotal: 120.Milliseconds(), maxTries: 1);
                shared2.GetCopy().Name.Should().Be("2000");
            }
        }
    }
}
