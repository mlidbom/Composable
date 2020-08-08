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
    public class MonitorCEPerformanceTests
    {
        class MyLong
        {
            long _value;
            public long Value => _value;

            readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();

            internal long Read_Unsafe() => _value;

            internal long Read_Locked()
            {
                lock(_monitor) return Read_Unsafe();
            }

            internal long Read_MonitorCE_Using_EnterLock()
            {
                using(_monitor.EnterReadLock())
                {
                    return Read_Unsafe();
                }
            }

            internal long Read_MonitorCE_Read() => _monitor.Read(Read_Unsafe);


            internal void Increment_Unsafe() => _value++;

            internal void Increment_Locked()
            {
                lock(_monitor) Increment_Unsafe();
            }

            internal void Increment_MonitorCE_Using_EnterLock()
            {
                using(_monitor.EnterReadLock()) Increment_Unsafe();
            }

            internal void Increment_MonitorCE_Using_EnterNotifyOneUpdateLock()
            {
                using(_monitor.EnterNotifyOnlyOneUpdateLock()) Increment_Unsafe();
            }

            internal void Increment_MonitorCE_Using_EnterNotifyAllUpdateLock()
            {
                using(_monitor.EnterNotifyOnlyOneUpdateLock()) Increment_Unsafe();
            }

            internal void Increment_MonitorCE_Update() => _monitor.Update(Increment_Unsafe);
        }

        MyLong _guarded;
        static readonly long TotalLocks = 10_000_000.EnvDivide(unoptimized: 10, instrumented: 100);
        const int Iterations = 100;
        static readonly long LocksPerIteration = TotalLocks / Iterations;

        [SetUp] public void SetupTask() => _guarded = new MyLong();

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
        static void RunScenarios(Action action, TimeSpan singleThread, TimeSpan multiThread)
        {
            RunSingleThreadedScenario(action, singleThread: singleThread);
            RunMultiThreadedScenario(action, multiThread: multiThread);
        }

        [Test] public void _010_Read_Unsafe________________________time_is_less_than_nanoseconds_SingleThreaded_06_MultiThreaded_01() =>
            RunScenarios(() => _guarded.Read_Unsafe(),
                         singleThread: (6 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 40, unoptimized: 1.4),
                         multiThread: (1 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 120));

        [Test] public void _020_Read_Locked________________________time_is_less_than_nanoseconds_SingleThreaded_25_MultiThreaded_180() =>
            RunScenarios(() => _guarded.Read_Locked(),
                         singleThread: (25 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 18, unoptimized: 1.8),
                         multiThread: (180 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 3.5));

        [Test] public void _031_Read_MonitorCE_Using_EnterLock______time_is_less_than_nanoseconds_SingleThreaded_25_MultiThreaded_180() =>
            RunScenarios(() => _guarded.Read_MonitorCE_Using_EnterLock(),
                         singleThread: (25 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 40, unoptimized: 2.2),
                         multiThread: (180 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 6.0, unoptimized:1.4));

        [Test] public void _032_Read_MonitorCE_Read________________time_is_less_than_nanoseconds_SingleThreaded_40_MultiThreaded_200() =>
            RunScenarios(() => _guarded.Read_MonitorCE_Read(),
                         singleThread: (40 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 24.0, unoptimized: 2.2),
                         multiThread: (200 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 7.0, unoptimized:1.6));

        [Test] public void _050_Increment_Unsafe___________________________________time_is_less_than_nanoseconds_SingleThreaded_06_MultiThreaded_08() =>
            RunScenarios(() => _guarded.Increment_Unsafe(),
                         singleThread: (6 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 40, unoptimized: 2.6),
                         multiThread: (8 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 20, unoptimized: 1.6));

        [Test] public void _060_Increment_Locked___________________________________time_is_less_than_nanoseconds_SingleThreaded_25__MultiThreaded_240() =>
            RunScenarios(() => _guarded.Increment_Locked(),
                         singleThread: (25 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8.0, unoptimized: 1.6),
                         multiThread: (240 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 3.0));

        [Test] public void _070_Increment_MonitorCE_Using_EnterLock_________________time_is_less_than_nanoseconds_SingleThreaded_30__MultiThreaded_240() =>
            RunScenarios(() => _guarded.Increment_MonitorCE_Using_EnterLock(),
                         singleThread: (30 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 32, unoptimized: 2.2),
                         multiThread: (240 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 5.0, unoptimized: 1.3));

        [Test] public void _071_Increment_MonitorCE_Using_EnterNotifyOneUpdateLock___time_is_less_than_nanoseconds_SingleThreaded_30__MultiThreaded_270() =>
            RunScenarios(() => _guarded.Increment_MonitorCE_Using_EnterNotifyOneUpdateLock(),
                         singleThread: (30 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 40, unoptimized: 2.8),
                         multiThread: (270 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8.0, unoptimized: 1.3));

        [Test] public void _072_Increment_MonitorCE_Using_EnterNotifyAllUpdateLock____time_is_less_than_nanoseconds_SingleThreaded_30__MultiThreaded_270() =>
            RunScenarios(() => _guarded.Increment_MonitorCE_Using_EnterNotifyAllUpdateLock(),
                         singleThread: (30 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 45, unoptimized: 2.8),
                         multiThread: (270 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8, unoptimized: 1.3));

        [Test] public void _073_Increment_MonitorCE_Update_________________________time_is_less_than_nanoseconds_SingleThreaded_50__MultiThreaded_250() =>
            RunScenarios(() => _guarded.Increment_MonitorCE_Update(),
                         singleThread: (50 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 32, unoptimized: 1.8),
                         multiThread: (250 * TotalLocks).Nanoseconds().EnvMultiply(instrumented: 8.0, unoptimized: 1.3));

    }
}
