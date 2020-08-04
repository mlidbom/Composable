using System;
using System.Threading.Tasks;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.Testing.Performance;
using Composable.Testing;
using NCrunch.Framework;
using NUnit.Framework;

// ReSharper disable InconsistentlySynchronizedField

namespace Composable.Tests.System.Threading.ResourceAccess
{
    [Performance, Serial] public class ResourceAccessGuardPerformanceTests
    {
        class MyLong
        {
            internal long Value = 0;
            internal void Increment() => Value++;

            readonly object _lock = new object();

            internal long Read() => Value;
        }

        class MyFakeGuard
        {
            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TResult Read<TResult>(Func<TResult> read) => read();

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Update(Action action) {}

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TResult Update<TResult>(Func<TResult> update) => update();
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

            TimeAsserter.Execute(HammerScenario, description: "Uncontended", maxTotal: mUnContended);
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

            TimeAsserter.ExecuteThreadedLowOverhead(HammerScenario, Iterations, description: "Contended", maxTotal: maxContended);
        }

        static void RunScenarios(Action action, TimeSpan mUnContended, TimeSpan maxContended)
        {
            RunSingleThreadedScenario(action, mUnContended: mUnContended);
            RunMultiThreadedScenario(action, maxContended: maxContended);
        }

        [Test] public void Average_LockedRead_time_is_less_than_30_nanoseconds() =>
            RunScenarios(() => _locker.Read(_guarded.Read),
                         mUnContended: (30 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8),
                         maxContended: (250 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8));

        [Test] public void Average_LockedIncrement_time_is_less_than_30_nanoseconds() =>
            RunScenarios(() => _locker.Update(_guarded.Increment),
                         mUnContended: (30 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8),
                         maxContended: (250 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8));

        [Test] public void Average_MonitorCE_Read_time_is_less_than_32_nanoseconds() =>
            RunScenarios(() => _monitor.Read(_guarded.Read),
                         mUnContended: (35 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8),
                         maxContended: (300 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8));

        [Test] public void Average_MonitorCE_Increment_time_is_less_than_35_nanoseconds() =>
            RunScenarios(() => _monitor.Update(_guarded.Increment),
                         mUnContended: (35 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8),
                         maxContended: (300 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8));

        [Test] public void Average_successful_TryEnter_time_is_less_than_20_nanoseconds() =>
            RunScenarios(() => _monitor.TryEnterNonBlocking(),
                         mUnContended: (20 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(6),
                         maxContended: (20 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(6));

        [Test] public void Average_failed_TryEnter_time_is_less_than_15_nanoseconds()
        {
            try
            {
                _monitor.Enter();
                Task.Run(() => RunScenarios(() => _monitor.TryEnterNonBlocking(),
                                            mUnContended: (15 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8),
                                            maxContended: (15 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8))).Wait();
            }
            finally
            {
                _monitor.Exit();
            }
        }

        [Test] public void Average_Update_time_is_less_than_200_nanoseconds() =>
            RunScenarios(() => _guard.Update(_guarded.Increment),
                         mUnContended: (200 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8),
                         maxContended: (400 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8));

        [Test] public void Average_Read_time_is_less_than_200_nanoseconds() =>
            RunScenarios(() => _guard.Read(_guarded.Read),
                         mUnContended: (100 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8),
                         maxContended: (200 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(3));

        [Test] public void Average_FakeUpdate_time_is_less_than_35_nanoseconds() =>
            RunScenarios(() => _fakeGuard.Update(_guarded.Increment),
                         mUnContended: (30 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8),
                         maxContended: (30 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8));

        [Test] public void Average_FakeRead_time_is_less_than_30_nanoseconds() =>
            RunScenarios(() => _fakeGuard.Read(_guarded.Read),
                         mUnContended: (30 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8),
                         maxContended: (30 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8));

        [Test] public void Average_UpdateLock_time_is_less_than_1_microsecond() =>
            RunScenarios(() =>
                         {
                             using(_guard.AwaitUpdateLock()) DoNothing();
                         },
                         mUnContended: (250 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8),
                         maxContended: TotalLocks.Microseconds().IfInstrumentedMultiplyBy(3));

        void DoNothing()
        {
            if(_doSomething)
            {
                Console.WriteLine("Something");
            }
        }
    }
}
