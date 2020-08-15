using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.SystemCE.ThreadingCE.TasksCE;
using FluentAssertions;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

// ReSharper disable AccessToDisposedClosure

namespace Composable.Tests.SystemCE.ThreadingCE.ResourceAccess
{
    [TestFixture] public class MonitorCE_specification
    {
        [Test] public void When_one_thread_has_UpdateLock_other_thread_is_blocked_until_first_thread_disposes_lock_()
        {
            var monitor = MonitorCE.WithTimeout(1.Seconds());

            var updateLock = monitor.EnterUpdateLock();

            using var otherThreadIsWaitingForLock = new ManualResetEventSlim(false);
            using var otherThreadGotLock = new ManualResetEventSlim(false);
            var otherThreadTask = TaskCE.Run(() =>
            {
                otherThreadIsWaitingForLock.Set();
                using(monitor.EnterUpdateLock())
                {
                    otherThreadGotLock.Set();
                }
            });

            otherThreadIsWaitingForLock.Wait();
            otherThreadGotLock.Wait(10.Milliseconds()).Should().BeFalse();

            updateLock.Dispose();
            otherThreadGotLock.Wait(10.Milliseconds()).Should().BeTrue();

            Task.WaitAll(otherThreadTask);
        }

        [Test] public void When_one_thread_calls_AwaitUpdateLock_twice_an_exception_is_thrown()
        {
            var monitor = MonitorCE.WithTimeout(1.Seconds());

            using(monitor.EnterUpdateLock())
            {
                Assert.Throws<InvalidOperationException>(() => monitor.EnterUpdateLock());
            }

        }

        [TestFixture] public class Given_a_timeout_of_10_milliseconds_an_exception_is_thrown_By_Get_within_15_milliseconds_if_lock_is_not_acquired
        {
            [Test] public void Exception_is_ObjectLockTimedOutException()
                => RunWithChangedStackTraceTimeout(
                    fetchStackTraceTimeout: 60.Milliseconds(),
                    () => RunScenario(ownerThreadWaitTime:15.Milliseconds(), monitorTimeout:5.Milliseconds()).Should().BeOfType<EnterLockTimeoutException>());

            [Test] public void If_owner_thread_blocks_for_less_than_stackTrace_timeout_Exception_contains_owning_threads_stack_trace()
                => RunWithChangedStackTraceTimeout(
                    fetchStackTraceTimeout: 60.Milliseconds(),
                    () => RunScenario(ownerThreadWaitTime: 15.Milliseconds(), monitorTimeout: 5.Milliseconds()).Message.Should().Contain(nameof(DisposeOwningThreadLock)));

            [Test] public void If_owner_thread_blocks_for_more_than_stacktrace_timeout__Exception_does_not_contain_owning_threads_stack_trace()
            {
                RunWithChangedStackTraceTimeout(
                    fetchStackTraceTimeout: 10.Milliseconds(),
                    () => RunScenario(ownerThreadWaitTime:20.Milliseconds(), monitorTimeout: 5.Milliseconds()).Message.Should().NotContain(nameof(DisposeOwningThreadLock)));
            }

            internal static void DisposeOwningThreadLock(IDisposable disposable) => disposable.Dispose();

            static Exception RunScenario(TimeSpan ownerThreadWaitTime, TimeSpan monitorTimeout)
            {
                var resourceGuard = MonitorCE.WithTimeout(monitorTimeout);

                var hasTakenLock = new ManualResetEvent(false);
                var isAwaitingLock = new ManualResetEvent(false);

                TaskCE.Run(() =>
                {
                    var @lock = resourceGuard.EnterUpdateLock();
                    hasTakenLock.Set();
                    isAwaitingLock.WaitOne();
                    Thread.Sleep(ownerThreadWaitTime);
                    DisposeOwningThreadLock(@lock);
                });

                hasTakenLock.WaitOne();

                var thrownException = Assert.Throws<AggregateException>(
                                                 () => TaskCE.Run(() =>
                                                              {
                                                                  isAwaitingLock.Set();
                                                                  resourceGuard.EnterUpdateLock();
                                                              })
                                                             .Wait())
                                            .InnerExceptions.Single();

                return thrownException;
            }

            static void RunWithChangedStackTraceTimeout(TimeSpan fetchStackTraceTimeout, Action action)
            {
                var timeoutProperty = typeof(EnterLockTimeoutException).GetField("_timeToWaitForOwningThreadStacktrace", BindingFlags.Static | BindingFlags.NonPublic)!;
                var original = timeoutProperty.GetValue(null);
                using(DisposableCE.Create(() => timeoutProperty.SetValue(null, original)))
                {
                    timeoutProperty.SetValue(null, fetchStackTraceTimeout);
                    action();
                }
            }
        }
    }
}
