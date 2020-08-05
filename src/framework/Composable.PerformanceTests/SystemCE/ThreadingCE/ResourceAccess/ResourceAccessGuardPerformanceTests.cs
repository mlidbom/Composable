using System;
using System.Threading.Tasks;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.Testing;
using Composable.Testing.Performance;
using NUnit.Framework;

// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable MemberCanBeMadeStatic.Local
// ReSharper disable UnusedParameter.Local

// ReSharper disable InconsistentlySynchronizedField

namespace Composable.Tests.SystemCE.ThreadingCE.ResourceAccess
{
    public class ResourceAccessGuardPerformanceTests
    {
        class MyLong
        {
            long _value;
            internal void Increment() => _value++;

            internal long Read() => _value;
        }

        class MyFakeGuard
        {
            public TResult Read<TResult>(Func<TResult> read) => read();

            public void Update(Action action) {}
        }

        class Locker
        {
            readonly object _lock = new object();
            public TResult Read<TResult>(Func<TResult> read)
            {
                lock(_lock) return read();
            }

            public void Update(Action action)
            {
                lock(_lock) action();
            }
        }

        bool _doSomething = true;
        MyLong _guarded;
        Locker _locker;
        MonitorCE _monitor;
        IResourceGuard _guard;
        MyFakeGuard _fakeGuard;
        const int TotalLocks = 1_000_000;
        const int Iterations = 100;
        const int LocksPerIteration = TotalLocks / Iterations;

        [SetUp] public void SetupTask()
        {
            _locker = new Locker();
            _monitor = MonitorCE.WithDefaultTimeout();
            _guard = ResourceGuard.WithDefaultTimeout();
            _guarded = new MyLong();
            _fakeGuard = new MyFakeGuard();
            _doSomething = false;
        }

        static void RunSingleThreadedScenario(Action action, TimeSpan mUnContended)
        {
            //ncrunch: no coverage start
            void HammerScenario()
            {
                for(var i = 0; i < TotalLocks; i++)
                    action();
            }
            //ncrunch: no coverage end

            TimeAsserter.Execute(HammerScenario, description: "Singlethreaded", maxTotal: mUnContended);
        }

        public static void RunMultiThreadedScenario(Action action, TimeSpan maxContended)
        {
            //ncrunch: no coverage start
            void HammerScenario()
            {
                for(var i = 0; i < LocksPerIteration; i++)
                    action();
            }
            //ncrunch: no coverage end

            TimeAsserter.ExecuteThreadedLowOverhead(HammerScenario, Iterations, description: "Multithreaded", maxTotal: maxContended);
        }

        // ReSharper disable once IdentifierTypo I want the numbers aligned in the calls
        static void RunScenarios(Action action, TimeSpan singleThread, TimeSpan multipThread)
        {
            RunSingleThreadedScenario(action, mUnContended: singleThread);
            RunMultiThreadedScenario(action, maxContended: multipThread);
        }

        [Test] public void LockedRead_time_is_less_than_nanoseconds_SingleThreaded__MultiThreaded_() =>
            RunScenarios(() => _locker.Read(_guarded.Read),
                         singleThread: (30 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8, unoptimized: 1.4),
                         multipThread: (250 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8));

        [Test] public void LockedIncrement_time_is_less_than_nanoseconds_SingleThreaded__MultiThreaded_() =>
            RunScenarios(() => _locker.Update(_guarded.Increment),
                         singleThread: (30 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8, unoptimized: 1.4),
                         multipThread: (250 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8));

        [Test] public void MonitorCE_Read_time_is_less_than_nanoseconds_SingleThreaded__MultiThreaded_() =>
            RunScenarios(() => _monitor.Read(_guarded.Read),
                         singleThread: (35 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 24, unoptimized: 1.7),
                         multipThread: (300 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8));

        [Test] public void MonitorCE_Increment_time_is_less_than_nanoseconds_SingleThreaded__MultiThreaded_() =>
            RunScenarios(() => _monitor.Update(_guarded.Increment),
                         singleThread: (35 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 24, unoptimized: 1.8),
                         multipThread: (300 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8));

        [Test] public void TryEnter_time_is_less_than_nanoseconds_SingleThreaded__MultiThreaded_() =>
            RunScenarios(() => _monitor.TryEnterNonBlocking(),
                         singleThread: (20 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 6),
                         multipThread: (8 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 6));

        [Test] public void TryEnter_failed_time_is_less_than_nanoseconds_SingleThreaded__MultiThreaded_()
        {
            try
            {
                _monitor.Enter();
                Task.Run(() => RunScenarios(() => _monitor.TryEnterNonBlocking(),
                                            singleThread: (15 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8),
                                            multipThread: (2 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8))).Wait();
            }
            finally
            {
                _monitor.Exit();
            }
        }

        [Test] public void Update_time_is_less_than_nanoseconds_SingleThreaded__MultiThreaded_() =>
            RunScenarios(
                //ncrunch: no coverage start
                () => _guard.Update(_guarded.Increment),
                //ncrunch: no coverage end
                singleThread: (200 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8),
                multipThread: (400 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8));

        [Test] public void Read_time_is_less_than_nanoseconds_SingleThreaded__MultiThreaded_() =>
            RunScenarios(() => _guard.Read(_guarded.Read),
                         singleThread: (100 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 10),
                         multipThread: (200 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 5, unoptimized: 1.8));

        [Test] public void FakeUpdate_time_is_less_than_nanoseconds_SingleThreaded_30_MultiThreaded_30() =>
            RunScenarios(() => _fakeGuard.Update(_guarded.Increment),
                         singleThread: (30 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8),
                         multipThread: (30 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8));

        [Test] public void FakeRead_time_is_less_than_nanoseconds_SingleThreaded_25_MultiThreaded_18() =>
            RunScenarios(() => _fakeGuard.Read(_guarded.Read),
                         singleThread: (25 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8),
                         multipThread: (18 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8));

        [Test] public void UpdateLock_time_is_less_than_nanoseconds_SingleThreaded_250_MultiThreaded_() =>
            RunScenarios(
                //ncrunch: no coverage start
                () =>
                {
                    using(_guard.AwaitUpdateLock()) DoNothing();
                },
                //ncrunch: no coverage end
                singleThread: (125 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8),
                multipThread: (100 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 3));

        void DoNothing()
        {
            if(_doSomething)
            {
                Console.WriteLine("Something");
            }
        }
    }
}
