using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Composable.Logging;
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
            readonly bool _doSomething;
            readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();
            long _value;
            public long Value => _value;
            public MyLong(in bool doSomething) => _doSomething = doSomething;


            internal void Increment() => _value++;
            internal long Read() => _value;

            internal void Increment_Locked()
            {
                lock(_monitor) _value++;
            }

            internal long Read_Locked()
            {
                lock(_monitor) return _value;
            }

            internal void Increment_MonitorCE_Lock()
            {
                using(_monitor.EnterLock())
                {
                    _value++;
                }
            }

            internal long Read_MonitorCE_Lock()
            {
                using(_monitor.EnterLock())
                {
                    return _value;
                }
            }
        }

        class MyFakeGuard
        {
            public TResult Read<TResult>(Func<TResult> read) => read();

            public void Update(Action action) => action();
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
        static readonly long TotalLocks = 10_000_000.EnvDivide(unoptimized:1000);
        const int Iterations = 100;
        static readonly long LocksPerIteration = TotalLocks / Iterations;

        [SetUp] public void SetupTask()
        {
            _doSomething = false;
            _locker = new Locker();
            _monitor = MonitorCE.WithDefaultTimeout();
            _guard = ResourceGuard.WithDefaultTimeout();
            _guarded = new MyLong(_doSomething);
            _fakeGuard = new MyFakeGuard();
        }

        [TearDown] public void TearDownTask()
        {
            ConsoleCE.WriteImportantLine(StringCE.Invariant($"{_guarded.Value:N0}"));
        }

        static void RunSingleThreadedScenario(Action action, TimeSpan mUnContended)
        {
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

        [Test] public void _010_Read_Unsafe____time_is_less_than_nanoseconds_SingleThreaded_35_MultiThreaded_25() =>
            RunScenarios(() => _guarded.Read(),
                         singleThread: (35 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 9.0, unoptimized: 1.4),
                         multiThread_: (25 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 5.0));

        [Test] public void _020_Read_Lock______time_is_less_than_nanoseconds_SingleThreaded_40_MultiThreaded_230() =>
            RunScenarios(() => _locker.Read(_guarded.Read),
                         singleThread: (40 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8, unoptimized: 1.8),
                         multiThread_: (220 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 3.5));

        [Test] public void _030_Read_MonitorCE_time_is_less_than_nanoseconds_SingleThreaded_45_MultiThreaded_250() =>
            RunScenarios(() => _monitor.Read(_guarded.Read),
                         singleThread: (45 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 24, unoptimized: 1.9),
                         multiThread_: (250 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 4));

        [Test] public void _040_Read_Guard____time_is_less_than_nanoseconds_SingleThreaded_70_MultiThreaded_220() =>
            RunScenarios(() => _guard.Read(_guarded.Read),
                         singleThread: (70 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 17, unoptimized: 1.6),
                         multiThread_: (220 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 5, unoptimized: 1.8));

        [Test] public void _050_Increment_Unsafe____time_is_less_than_nanoseconds_SingleThreaded_20__MultiThreaded_25() =>
            RunScenarios(() => _guarded.Increment(),
                         singleThread: (20 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 9, unoptimized:2.4),
                         multiThread_: (25 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 4.5));

        [Test] public void _060_Increment_Lock______time_is_less_than_nanoseconds_SingleThreaded_45__MultiThreaded_280() =>
            RunScenarios(() => _locker.Update(_guarded.Increment),
                         singleThread: (45 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8, unoptimized: 1.6),
                         multiThread_: (280 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 2.2));

        [Test] public void _070_Increment_MonitorCE_time_is_less_than_nanoseconds_SingleThreaded_60__MultiThreaded_250() =>
            RunScenarios(() => _monitor.Update(_guarded.Increment),
                         singleThread: (60 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 32, unoptimized: 1.8),
                         multiThread_: (250 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 5.0, unoptimized: 1.3));

        [Test] public void _080_Increment_Guard____time_is_less_than_nanoseconds_SingleThreaded_130_MultiThreaded_300() =>
            RunScenarios(() => _guard.Update(_guarded.Increment),
                         singleThread: (130 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 12, unoptimized: 1.3),
                         multiThread_: (300 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 6, unoptimized: 1.4));

        [Test] public void _090_MonitorCE_TryEnter_time_is_less_than_nanoseconds_SingleThreaded_14_MultiThreaded_4() =>
            RunScenarios(() => _monitor.TryEnterNonBlocking(),
                         singleThread: (14 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 9, unoptimized: 1.3),
                         multiThread_: (4 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 23, unoptimized: 1.7));

        [Test] public void _100_MonitorCE_TryEnter_failed_time_is_less_than_nanoseconds_SingleThreaded_12_MultiThreaded_1_85()
        {
            try
            {
                _monitor.Enter();
                Task.Run(() => RunScenarios(() => _monitor.TryEnterNonBlocking(),
                                            singleThread: (12 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 12, unoptimized: 1.4),
                                            multiThread_: (1.85 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 45, unoptimized: 1.5))).Wait();
            }
            finally
            {
                _monitor.Exit();
            }
        }

        [Test] public void _110_Guard_AwaitUpdateLock_time_is_less_than_nanoseconds_SingleThreaded_250_MultiThreaded_() =>
            RunScenarios(
                //ncrunch: no coverage start
                () =>
                {
                    using(_guard.AwaitUpdateLock()) DoNothing();
                },
                //ncrunch: no coverage end
                singleThread: (90 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 22, unoptimized: 1.4),
                multiThread_: (200 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 22, unoptimized: 1.6));

        //ncrunch: no coverage start
        void DoNothing()
        {
            if(_doSomething)
            {
                Console.WriteLine("Something");
            }
        }
        //ncrunch: no coverage end
    }
}
