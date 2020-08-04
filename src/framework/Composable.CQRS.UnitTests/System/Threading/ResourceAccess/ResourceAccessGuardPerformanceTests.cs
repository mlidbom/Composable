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

            public TResult Update<TResult>(Func<TResult> update)
            {
                lock(_lock) return update();
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

        static void RunSingleThreadedScenario(Action action, TimeSpan maxTotal)
        {
            //ncrunch: no coverage start
            void HammerScenario()
            {
                for(var i = 0; i < TotalLocks; i++)
                    action();
            }
            //ncrunch: no coverage end

            TimeAsserter.Execute(HammerScenario, maxTotal: maxTotal);
        }

        public static void RunMultiThreadedScenario(Action action, TimeSpan maxTotal)
        {
            //ncrunch: no coverage start
            void HammerScenario()
            {
                for(var i = 0; i < LocksPerIteration; i++)
                    action();
            }
            //ncrunch: no coverage end

            TimeAsserter.ExecuteThreadedLowOverhead(HammerScenario, Iterations, maxTotal: maxTotal);
        }

        [Test] public void Average_failed_TryEnter_time_is_less_than_15_nanoseconds()
        {
            try
            {
                _monitor.Enter();
                Task.Run(() => RunMultiThreadedScenario(() => _monitor.TryEnterNonBlocking(), maxTotal: (15 * TotalLocks).IfInstrumentedMultiplyBy(8).Nanoseconds()));
            }
            finally
            {
                _monitor.Exit();
            }
        }

        [Test] public void Average_uncontended_LockedRead_time_is_less_than_30_nanoseconds() =>
            RunSingleThreadedScenario(() => _locker.Read(_guarded.Read), maxTotal: (23 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8));

        [Test] public void Average_uncontended_LockedIncrement_time_is_less_than_30_nanoseconds() =>
            RunSingleThreadedScenario(() => _locker.Update(_guarded.Increment), maxTotal: (23 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8));

        [Test] public void Average_uncontended_MonitorCE_Read_time_is_less_than_32_nanoseconds() =>
            RunSingleThreadedScenario(() => _monitor.Read(_guarded.Read), maxTotal: (27 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8));

        [Test] public void Average_uncontended_MonitorCE_Increment_time_is_less_than_35_nanoseconds() =>
            RunSingleThreadedScenario(() => _monitor.Update(_guarded.Increment), maxTotal: (28 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8));

        [Test] public void Average_successful_TryEnter_time_is_less_than_20_nanoseconds() =>
            RunSingleThreadedScenario(() => _monitor.TryEnterNonBlocking(), maxTotal: (20 * TotalLocks).IfInstrumentedMultiplyBy(6).Nanoseconds());

        [Test] public void Average_uncontended_Update_time_is_less_than_200_nanoseconds() =>
            RunSingleThreadedScenario(() => _guard.Update(_guarded.Increment), maxTotal: (200 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8));

        [Test] public void Average_contended_Update_time_is_less_than_1_microsecond() =>
            RunMultiThreadedScenario(() => _guard.Update(_guarded.Increment), maxTotal: TotalLocks.Microseconds().IfInstrumentedMultiplyBy(1.5));

        [Test] public void Average_contended_Read_time_is_less_than_200_nanoseconds() =>
            RunMultiThreadedScenario(() => _guard.Read(_guarded.Read), maxTotal: (200 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(3));

        [Test] public void Average_uncontended_Read_time_is_less_than_150_nanoseconds() =>
            RunSingleThreadedScenario(() => _guard.Read(_guarded.Read), maxTotal: (150 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8));

        [Test] public void Average_uncontended_FakeUpdate_time_is_less_than_35_nanoseconds() =>
            RunSingleThreadedScenario(() => _fakeGuard.Update(_guarded.Increment), maxTotal: (30 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8));

        [Test] public void Average_uncontended_FakeRead_time_is_less_than_30_nanoseconds() =>
            RunSingleThreadedScenario(() => _fakeGuard.Read(_guarded.Read), maxTotal: (30 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8));

        [Test] public void Average_contended_UpdateLock_time_is_less_than_1_microsecond() =>
            RunMultiThreadedScenario(() =>
                                     {
                                         using(_guard.AwaitUpdateLock()) DoNothing();
                                     },
                                     maxTotal: TotalLocks.Microseconds().IfInstrumentedMultiplyBy(3));

        [Test] public void Average_uncontended_UpdateLock_time_is_less_than_250_nanoseconds() =>
            RunSingleThreadedScenario(() =>
                                      {
                                          using(_guard.AwaitUpdateLock()) DoNothing();
                                      },
                                      maxTotal: (250 * TotalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8));

        void DoNothing()
        {
            if(_doSomething)
            {
                Console.WriteLine("Something");
            }
        }
    }
}
