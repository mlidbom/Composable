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
        static readonly int TotalLocks = 1_000_000.EnvDivide(unoptimized:10);
        const int Iterations = 100;
        static readonly int LocksPerIteration = TotalLocks / Iterations;

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

        // ReSharper disable once InconsistentNaming
        static void RunScenarios(Action action, TimeSpan singleThread, TimeSpan multiThread_)
        {
            RunSingleThreadedScenario(action, mUnContended: singleThread);
            RunMultiThreadedScenario(action, maxContended: multiThread_);
        }

        [Test] public void LockedRead_time_is_less_than_nanoseconds_SingleThreaded__MultiThreaded_() =>
            RunScenarios(() => _locker.Read(_guarded.Read),
                         singleThread: (40 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8, unoptimized: 1.8),
                         multiThread_: (220 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 3.5));

        [Test] public void LockedIncrement_time_is_less_than_nanoseconds_SingleThreaded__MultiThreaded_() =>
            RunScenarios(() => _locker.Update(_guarded.Increment),
                         singleThread: (45 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8, unoptimized: 1.6),
                         multiThread_: (280 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 2.2));

        [Test] public void MonitorCE_Read_time_is_less_than_nanoseconds_SingleThreaded__MultiThreaded_() =>
            RunScenarios(() => _monitor.Read(_guarded.Read),
                         singleThread: (45 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 24, unoptimized: 1.9),
                         multiThread_: (250 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 4));

        [Test] public void MonitorCE_Increment_time_is_less_than_nanoseconds_SingleThreaded__MultiThreaded_() =>
            RunScenarios(() => _monitor.Update(_guarded.Increment),
                         singleThread: (60 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 32, unoptimized: 1.8),
                         multiThread_: (250 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 5.0, unoptimized: 1.3));

        [Test] public void TryEnter_time_is_less_than_nanoseconds_SingleThreaded__MultiThreaded_() =>
            RunScenarios(() => _monitor.TryEnterNonBlocking(),
                         singleThread: (14 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 9, unoptimized: 1.3),
                         multiThread_: (4 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 23, unoptimized: 1.7));

        [Test] public void TryEnter_failed_time_is_less_than_nanoseconds_SingleThreaded__MultiThreaded_()
        {
            try
            {
                _monitor.Enter();
                Task.Run(() => RunScenarios(
                             //ncrunch: no coverage start
                             () => _monitor.TryEnterNonBlocking(),
                             //ncrunch: no coverage end
                                            singleThread: (12 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 12, unoptimized: 1.4),
                                            multiThread_: (1.85 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 45, unoptimized: 1.5))).Wait();
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
                singleThread: (130 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 12, unoptimized: 1.3),
                multiThread_: (300 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 6, unoptimized: 1.4));

        [Test] public void Read_time_is_less_than_nanoseconds_SingleThreaded__MultiThreaded_() =>
            RunScenarios(() => _guard.Read(_guarded.Read),
                         singleThread: (70 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 17, unoptimized: 1.6),
                         multiThread_: (220 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 5, unoptimized: 1.8));

        [Test] public void FakeUpdate_time_is_less_than_nanoseconds_SingleThreaded_30_MultiThreaded_30() =>
            RunScenarios(() => _fakeGuard.Update(_guarded.Increment),
                         singleThread: (20 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 9, unoptimized:2.4),
                         multiThread_: (25 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 4.5));

        [Test] public void FakeRead_time_is_less_than_nanoseconds_SingleThreaded_25_MultiThreaded_18() =>
            RunScenarios(() => _fakeGuard.Read(_guarded.Read),
                         singleThread: (35 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 9.0, unoptimized: 1.4),
                         multiThread_: (25 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 5.0));

        [Test] public void UpdateLock_time_is_less_than_nanoseconds_SingleThreaded_250_MultiThreaded_() =>
            RunScenarios(
                //ncrunch: no coverage start
                () =>
                {
                    using(_guard.AwaitUpdateLock()) DoNothing();
                },
                //ncrunch: no coverage end
                singleThread: (90 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 22, unoptimized: 1.4),
                multiThread_: (200 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 22, unoptimized: 1.6));

        void DoNothing()
        {
            if(_doSomething)
            {
                Console.WriteLine("Something");
            }
        }
    }
}
