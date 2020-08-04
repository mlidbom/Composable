using System;
using System.Runtime.CompilerServices;
using System.Threading;
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

            internal long ReadLocked()
            {
                lock(_lock) return Value;
            }

            internal void IncrementLocked()
            {
                lock(_lock)Value++;
            }
        }

        class MyFakeGuard
        {
            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TResult Read<TResult>(Func<TResult> read) => read();

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Update(Action action) { }

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TResult Update<TResult>(Func<TResult> update) => update();
        }

        [SetUp] public void SetupTask() { _doSomething = false; }

        [Test] public void Average_failed_TryEnter_time_is_less_than_15_nanoseconds()
        {
            var guard = MonitorCE.WithTimeout(1.Seconds());

            const int totalLocks = 1_000_000;

            //ncrunch: no coverage start
            void HammerTryEnter()
            {
                for(var i = 0; i < totalLocks; i++)
                    guard.TryEnterNonBlocking();
            }
            //ncrunch: no coverage end

            try
            {
                guard.Enter();
                var time = Task.Run(() => TimeAsserter.Execute(HammerTryEnter, maxTotal: (15 * totalLocks).IfInstrumentedMultiplyBy(8).Nanoseconds())).Result;
            }
            finally
            {
                guard.Exit();
            }
        }

        [Test] public void Average_successful_TryEnter_time_is_less_than_20_nanoseconds()
        {
            var guard = MonitorCE.WithTimeout(1.Seconds());

            const int totalLocks = 1_000_000;

            //ncrunch: no coverage start
            void HammerTryEnter()
            {
                for(var i = 0; i < totalLocks; i++)
                    guard.TryEnterNonBlocking();
            }
            //ncrunch: no coverage end

            TimeAsserter.Execute(HammerTryEnter, maxTotal: (20 * totalLocks).IfInstrumentedMultiplyBy(6).Nanoseconds());
        }

        [Test] public void Average_uncontended_Update_time_is_less_than_200_nanoseconds()
        {
            var guard = ResourceGuard.WithTimeout(100.Milliseconds());

            const int totalLocks = 1_000_000;

            var guarded = new MyLong();

            //ncrunch: no coverage start
            void HammerUpdateLocks()
            {
                for(var i = 0; i < totalLocks; i++)
                    guard.Update(guarded.Increment);
            }
            //ncrunch: no coverage end

            TimeAsserter.Execute(HammerUpdateLocks, maxTotal: (200 * totalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8));
        }

        [Test] public void Average_contended_Update_time_is_less_than_1_microsecond()
        {
            var guard = ResourceGuard.WithTimeout(100.Milliseconds());

            const int totalLocks = 1_000_000;
            const int iterations = 100;
            const int locksPerIteration = totalLocks / iterations;

            var guarded = new MyLong();
            //ncrunch: no coverage start
            void HammerUpdateLocks()
            {
                for(var i = 0; i < locksPerIteration; i++)
                    guard.Update(guarded.Increment);
            }
            //ncrunch: no coverage end

            TimeAsserter.ExecuteThreadedLowOverhead(HammerUpdateLocks,
                                                    iterations,
                                                    description: $"Take {locksPerIteration} update locks",
                                                    maxTotal: totalLocks.Microseconds().IfInstrumentedMultiplyBy(1.5));
        }

        [Test] public void Average_contended_Read_time_is_less_than_1_microsecond()
        {
            var guard = ResourceGuard.WithTimeout(1.Seconds());

            const int totalLocks = 1_000_000;
            const int iterations = 100;
            const int locksPerIteration = totalLocks / iterations;
            var guarded = new MyLong();

            //ncrunch: no coverage start
            void HammerUpdateLocks()
            {
                for(var i = 0; i < locksPerIteration; i++)
                    guard.Read(guarded.Read);
            }
            //ncrunch: no coverage end

            TimeAsserter.ExecuteThreadedLowOverhead(HammerUpdateLocks,
                                                    iterations,
                                                    description: $"Take {locksPerIteration} update locks",
                                                    maxTotal: totalLocks.Microseconds().IfInstrumentedMultiplyBy(3));
        }

        [Test] public void Average_uncontended_Read_time_is_less_than_150_nanoseconds()
        {
            var guard = ResourceGuard.WithTimeout(100.Milliseconds());

            const int totalLocks = 1_000_000;

            var guarded = new MyLong();

            //ncrunch: no coverage start
            void HammerUpdateLocks()
            {
                for(var i = 0; i < totalLocks; i++)
                    guard.Read(guarded.Read);
            }
            //ncrunch: no coverage end

            TimeAsserter.Execute(HammerUpdateLocks, maxTotal: (150 * totalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8));
        }

        [Test] public void Average_uncontended_LockedRead_time_is_less_than_30_nanoseconds()
        {
            const int totalLocks = 1_000_000;

            var guarded = new MyLong();

            //ncrunch: no coverage start
            void HammerUpdateLocks()
            {
                for(var i = 0; i < totalLocks; i++)
                    guarded.ReadLocked();
            }
            //ncrunch: no coverage end

            TimeAsserter.Execute(HammerUpdateLocks, maxTotal: (30 * totalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8));
        }

        [Test] public void Average_uncontended_LockedIncrement_time_is_less_than_30_nanoseconds()
        {
            const int totalLocks = 1_000_000;

            var guarded = new MyLong();

            //ncrunch: no coverage start
            void HammerUpdateLocks()
            {
                for(var i = 0; i < totalLocks; i++)
                    guarded.IncrementLocked();
            }
            //ncrunch: no coverage end

            TimeAsserter.Execute(HammerUpdateLocks, maxTotal: (30 * totalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8));
        }

        [Test] public void Average_uncontended_FakeUpdate_time_is_less_than_30_nanoseconds()
        {
            const int totalLocks = 1_000_000;

            var guarded = new MyLong();
            var fakeGuard = new MyFakeGuard();

            //ncrunch: no coverage start
            void HammerUpdateLocks()
            {
                for(var i = 0; i < totalLocks; i++)
                    fakeGuard.Update(guarded.Increment);
            }
            //ncrunch: no coverage end

            TimeAsserter.Execute(HammerUpdateLocks, maxTotal: (30 * totalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8));
        }

        [Test] public void Average_uncontended_FakeRead_time_is_less_than_30_nanoseconds()
        {
            const int totalLocks = 1_000_000;

            var guarded = new MyLong();
            var fakeGuard = new MyFakeGuard();

            //ncrunch: no coverage start
            void HammerUpdateLocks()
            {
                for(var i = 0; i < totalLocks; i++)
                    fakeGuard.Read(guarded.Read);
            }
            //ncrunch: no coverage end

            TimeAsserter.Execute(HammerUpdateLocks, maxTotal: (30 * totalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8));
        }

        [Test] public void Average_contended_UpdateLock_time_is_less_than_1_microsecond()
        {
            var guard = ResourceGuard.WithTimeout(1.Seconds());

            const int totalLocks = 1_000_000;
            const int iterations = 100;
            const int locksPerIteration = totalLocks / iterations;

            //ncrunch: no coverage start
            void HammerUpdateLocks()
            {
                for(var i = 0; i < locksPerIteration; i++)
                    using(guard.AwaitUpdateLock())
                    {
                        DoNothing();
                    }
            }
            //ncrunch: no coverage end

            TimeAsserter.ExecuteThreadedLowOverhead(HammerUpdateLocks,
                                                    iterations,
                                                    description: $"Take {locksPerIteration} update locks",
                                                    maxTotal: totalLocks.Microseconds().IfInstrumentedMultiplyBy(3));
        }

        [Test] public void Average_uncontended_UpdateLock_time_is_less_than_250_nanoseconds()
        {
            var guard = ResourceGuard.WithTimeout(100.Milliseconds());

            const int totalLocks = 1_000_000;

            //ncrunch: no coverage start
            void HammerUpdateLocks()
            {
                for(var i = 0; i < totalLocks; i++)
                    using(guard.AwaitUpdateLock())
                    {
                        DoNothing();
                    }
            }
            //ncrunch: no coverage end

            TimeAsserter.Execute(HammerUpdateLocks, maxTotal: (250 * totalLocks).Nanoseconds().IfInstrumentedMultiplyBy(8));
        }

        bool _doSomething = true;
        void DoNothing()
        {
            if(_doSomething)
            {
                Console.WriteLine("Something");
            }
        }

        readonly object _readObject = new object();
        object ReadNothing()
        {
            if(_doSomething)
            {
                return _readObject;
            } else
            {
                return null!;
            }
        }
    }
}
