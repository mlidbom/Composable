using System;
using Composable.System;
using Composable.System.Threading;
using Composable.Testing.Performance;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.System.Threading
{
    [TestFixture] public class PersistentMachineWideSharedObjectPerformanceTests
    {
        [Test] public void Get_copy_runs_single_threaded_1000_times_in_60_milliseconds()
        {
            var name = Guid.NewGuid().ToString();
            using(var shared = MachineWideSharedObject<SharedObject>.For(name, usePersistentFile: true))
            using(var shared2 = MachineWideSharedObject<SharedObject>.For(name, usePersistentFile: true))
            {
                TimeAsserter.Execute(() => shared.GetCopy(), iterations: 1000, maxTotal: 60.Milliseconds());
                TimeAsserter.Execute(() => shared2.GetCopy(), iterations: 1000, maxTotal: 60.Milliseconds());
            }
        }

        [Test] public void Get_copy_runs_multi_threaded_1000_times_in_100_milliseconds()
        {
            var name = Guid.NewGuid().ToString();
            using(var shared = MachineWideSharedObject<SharedObject>.For(name, usePersistentFile: true))
            using(var shared2 = MachineWideSharedObject<SharedObject>.For(name, usePersistentFile: true))
            {
                TimeAsserter.ExecuteThreaded(() => shared.GetCopy(), iterations: 1000, maxTotal: 100.Milliseconds());
                TimeAsserter.ExecuteThreaded(() => shared2.GetCopy(), iterations: 1000, maxTotal: 100.Milliseconds());
            }
        }

        [Test] public void Update_runs_single_threaded_1000_times_in_170_milliseconds()
        {
            var name = Guid.NewGuid().ToString();
            using(var shared = MachineWideSharedObject<SharedObject>.For(name, usePersistentFile: true))
            using(var shared2 = MachineWideSharedObject<SharedObject>.For(name, usePersistentFile: true))
            {
                int counter = 0;
                TimeAsserter.Execute(() => shared.Update(@this => @this.Name = (++counter).ToString()), iterations: 1000, maxTotal: 170.Milliseconds(), maxTries: 1);
                shared.GetCopy().Name.Should().Be("1000");
                TimeAsserter.Execute(() => shared2.Update(@this => @this.Name = (++counter).ToString()), iterations: 1000, maxTotal: 170.Milliseconds(), maxTries: 1);
                shared2.GetCopy().Name.Should().Be("2000");
            }
        }

        [Test] public void Update_runs_multi_threaded_1000_times_in_250_milliseconds()
        {
            var name = Guid.NewGuid().ToString();
            using(var shared = MachineWideSharedObject<SharedObject>.For(name, usePersistentFile: true))
            using(var shared2 = MachineWideSharedObject<SharedObject>.For(name, usePersistentFile: true))
            {
                int counter = 0;
                TimeAsserter.ExecuteThreaded(() => shared.Update(@this => @this.Name = (++counter).ToString()), iterations: 1000, maxTotal: 250.Milliseconds(), maxTries: 1);
                shared.GetCopy().Name.Should().Be("1000");
                TimeAsserter.ExecuteThreaded(() => shared2.Update(@this => @this.Name = (++counter).ToString()), iterations: 1000, maxTotal: 250.Milliseconds(), maxTries: 1);
                shared2.GetCopy().Name.Should().Be("2000");
            }
        }
    }
}
