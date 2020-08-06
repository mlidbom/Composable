using System;
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
            long _value;
            public long Value => _value;

            readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();
            readonly IResourceGuard _guard = ResourceGuard.WithDefaultTimeout();

            internal long Read_Unsafe() => _value;

            internal long Read_Locked()
            {
                lock(_monitor) return Read_Unsafe();
            }

            internal long Read_MonitorCE_Enter_Finally_Exit()
            {
                try
                {
                    _monitor.Enter();
                    return Read_Unsafe();
                }
                finally
                {
                    _monitor.Exit();
                }
            }

            internal long Read_MonitorCE_Using_EnterReadLock()
            {
                using(_monitor.EnterReadLock()) return Read_Unsafe();
            }

            internal long Read_MonitorCE_Using_EnterLock()
            {
                using(_monitor.EnterLock())
                {
                    return Read_Unsafe();
                }
            }

            internal long Read_MonitorCE_Read() => _monitor.Read(Read_Unsafe);

            internal long Read_MonitorCE_Read_Closure() => _monitor.Read(() => _value);

            internal long Read_Guard_Read() => _guard.Read(Read_Unsafe);

            internal void Increment_Unsafe() => _value++;

            internal void Increment_Locked()
            {
                lock(_monitor) Increment_Unsafe();
            }

            internal void Increment_MonitorCE_Enter_Finally_Exit() //Here basically just to illustrate that it is no faster than the methods below using a using statement.
            {
                try
                {
                    _monitor.Enter();
                    Increment_Unsafe();
                }
                finally
                {
                    _monitor.Exit();
                }
            }

            internal void Increment_MonitorCE_Using_EnterLock()
            {
                using(_monitor.EnterLock()) Increment_Unsafe();
            }

            internal void Increment_MonitorCE_Using_EnterNotifyOneUpdateLock()
            {
                using(_monitor.EnterNotifyOneUpdateLock()) Increment_Unsafe();
            }

            internal void Increment_MonitorCE_Using_EnterNotifyAllUpdateLock()
            {
                using(_monitor.EnterNotifyOneUpdateLock()) Increment_Unsafe();
            }

            internal void Increment_MonitorCE_Update() => _monitor.Update(Increment_Unsafe);

            internal void Increment_MonitorCE_Update_Closure() => _monitor.Update(() => _value++);

            internal void Increment_Guard_Update() => _guard.Update(Increment_Unsafe);
        }

        MyLong _guarded;
        MonitorCE _monitor;
        static readonly long TotalLocks = 10_000_000.EnvDivide(unoptimized: 100, instrumented: 100);
        const int Iterations = 100;
        static readonly long LocksPerIteration = TotalLocks / Iterations;

        [SetUp] public void SetupTask()
        {
            _monitor = MonitorCE.WithDefaultTimeout();
            _guarded = new MyLong();
        }

        [TearDown] public void TearDownTask() { ConsoleCE.WriteImportantLine(StringCE.Invariant($"{_guarded.Value:N0}")); }

        static void RunSingleThreadedScenario(Action action, TimeSpan singleThread)
        {
            //ncrunch: no coverage start
            void HammerScenario()
            {
                for(var i = 0; i < TotalLocks; i++)
                    action();
            }
            //ncrunch: no coverage end

            TimeAsserter.Execute(HammerScenario, description: "Singlethreaded", maxTotal: singleThread);
        }

        public static void RunMultiThreadedScenario(Action action, TimeSpan multiThread)
        {
            //ncrunch: no coverage start
            void HammerScenario()
            {
                for(var i = 0; i < LocksPerIteration; i++)
                    action();
            }
            //ncrunch: no coverage end

            TimeAsserter.ExecuteThreadedLowOverhead(HammerScenario, Iterations, description: "Multithreaded", maxTotal: multiThread);
        }

        // ReSharper disable once InconsistentNaming
        static void RunScenarios(Action action, TimeSpan singleThread, TimeSpan multiThread_)
        {
            RunSingleThreadedScenario(action, singleThread: singleThread);
            RunMultiThreadedScenario(action, multiThread: multiThread_);
        }

        [Test] public void _010_Read_Unsafe________________________time_is_less_than_nanoseconds_SingleThreaded_06_MultiThreaded_01() =>
            RunScenarios(() => _guarded.Read_Unsafe(),
                         singleThread: (6 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 9.0, unoptimized: 1.4),
                         multiThread_: (1 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 5.0));

        [Test] public void _020_Read_Locked________________________time_is_less_than_nanoseconds_SingleThreaded_20_MultiThreaded_140() =>
            RunScenarios(() => _guarded.Read_Locked(),
                         singleThread: (20 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8, unoptimized: 1.8),
                         multiThread_: (140 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 3.5));

        [Test] public void _029_Read_MonitorCE_Enter_Finally_Exit_____time_is_less_than_nanoseconds_SingleThreaded_20_MultiThreaded_140() =>
            RunScenarios(() => _guarded.Read_MonitorCE_Enter_Finally_Exit(),
                         singleThread: (20 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 24, unoptimized: 1.9),
                         multiThread_: (140 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 4));

        [Test] public void _030_Read_MonitorCE_Using_EnterReadLock__time_is_less_than_nanoseconds_SingleThreaded_20_MultiThreaded_140() =>
            RunScenarios(() => _guarded.Read_MonitorCE_Using_EnterReadLock(),
                         singleThread: (20 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 24, unoptimized: 1.9),
                         multiThread_: (140 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 4));

        [Test] public void _031_Read_MonitorCE_Using_EnterLock______time_is_less_than_nanoseconds_SingleThreaded_20_MultiThreaded_140() =>
            RunScenarios(() => _guarded.Read_MonitorCE_Using_EnterLock(),
                         singleThread: (20 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 24, unoptimized: 1.9),
                         multiThread_: (140 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 4));

        [Test] public void _032_Read_MonitorCE_Read________________time_is_less_than_nanoseconds_SingleThreaded_35_MultiThreaded_250() =>
            RunScenarios(() => _guarded.Read_MonitorCE_Read(),
                         singleThread: (35 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 24, unoptimized: 1.9),
                         multiThread_: (160 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 4));

        [Test] public void _033_Read_MonitorCE_Read_Closure________time_is_less_than_nanoseconds_SingleThreaded_35_MultiThreaded_200() =>
            RunScenarios(() => _guarded.Read_MonitorCE_Read_Closure(),
                         singleThread: (35 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 24, unoptimized: 1.9),
                         multiThread_: (160 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 4));

        [Test] public void _040_Read_Guard_Read___________________time_is_less_than_nanoseconds_SingleThreaded_60_MultiThreaded_220() =>
            RunScenarios(() => _guarded.Read_Guard_Read(),
                         singleThread: (60 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 17, unoptimized: 1.6),
                         multiThread_: (220 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 5, unoptimized: 1.8));

        [Test] public void _050_Increment_Unsafe___________________________________time_is_less_than_nanoseconds_SingleThreaded_06_MultiThreaded_08() =>
            RunScenarios(() => _guarded.Increment_Unsafe(),
                         singleThread: (6 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 9, unoptimized: 2.4),
                         multiThread_: (8 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 4.5));

        [Test] public void _060_Increment_Locked___________________________________time_is_less_than_nanoseconds_SingleThreaded_20__MultiThreaded_200() =>
            RunScenarios(() => _guarded.Increment_Locked(),
                         singleThread: (20 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8, unoptimized: 1.6),
                         multiThread_: (200 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 2.2));

        [Test] public void _069_Increment_MonitorCE_Enter_Finally_Exit________________time_is_less_than_nanoseconds_SingleThreaded_20__MultiThreaded_200() =>
            RunScenarios(() => _guarded.Increment_MonitorCE_Enter_Finally_Exit(),
                         singleThread: (20 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 32, unoptimized: 1.8),
                         multiThread_: (200 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 5.0, unoptimized: 1.3));

        [Test] public void _070_Increment_MonitorCE_Using_EnterLock_________________time_is_less_than_nanoseconds_SingleThreaded_20__MultiThreaded_200() =>
            RunScenarios(() => _guarded.Increment_MonitorCE_Using_EnterLock(),
                         singleThread: (20 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 32, unoptimized: 1.8),
                         multiThread_: (200 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 5.0, unoptimized: 1.3));

        [Test] public void _071_Increment_MonitorCE_Using_EnterNotifyOneUpdateLock___time_is_less_than_nanoseconds_SingleThreaded_20__MultiThreaded_200() =>
            RunScenarios(() => _guarded.Increment_MonitorCE_Using_EnterNotifyOneUpdateLock(),
                         singleThread: (20 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 32, unoptimized: 1.8),
                         multiThread_: (200 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 5.0, unoptimized: 1.3));

        [Test] public void _072_Increment_MonitorCE_Using_EnterNotifyAllUpdateLock____time_is_less_than_nanoseconds_SingleThreaded_20__MultiThreaded_200() =>
            RunScenarios(() => _guarded.Increment_MonitorCE_Using_EnterNotifyAllUpdateLock(),
                         singleThread: (20 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 32, unoptimized: 1.8),
                         multiThread_: (200 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 5.0, unoptimized: 1.3));

        [Test] public void _073_Increment_MonitorCE_Update_________________________time_is_less_than_nanoseconds_SingleThreaded_50__MultiThreaded_250() =>
            RunScenarios(() => _guarded.Increment_MonitorCE_Update(),
                         singleThread: (50 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 32, unoptimized: 1.8),
                         multiThread_: (250 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 5.0, unoptimized: 1.3));

        [Test] public void _074_Increment_MonitorCE_Update__Closure_________________time_is_less_than_nanoseconds_SingleThreaded_50__MultiThreaded_250() =>
            RunScenarios(() => _guarded.Increment_MonitorCE_Update_Closure(),
                         singleThread: (50 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 32, unoptimized: 1.8),
                         multiThread_: (250 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 5.0, unoptimized: 1.3));

        [Test] public void _080_Increment_Guard_Update_____________________________time_is_less_than_nanoseconds_SingleThreaded_80_MultiThreaded_300() =>
            RunScenarios(() => _guarded.Increment_Guard_Update(),
                         singleThread: (80 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 12, unoptimized: 1.3),
                         multiThread_: (300 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 6, unoptimized: 1.4));

        [Test] public void _090_MonitorCE_TryEnter_time_is_less_than_nanoseconds_SingleThreaded_14_MultiThreaded_4() =>
            RunScenarios(() => _monitor.TryEnterNonBlocking(),
                         singleThread: (14 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 9, unoptimized: 1.3),
                         multiThread_: (4 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 23, unoptimized: 1.7));
    }
}
