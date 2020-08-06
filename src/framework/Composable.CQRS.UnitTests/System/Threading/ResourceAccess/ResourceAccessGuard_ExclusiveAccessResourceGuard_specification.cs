using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using FluentAssertions;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

// ReSharper disable AccessToDisposedClosure

namespace Composable.Tests.System.Threading.ResourceAccess
{
    [TestFixture] public class ResourceAccessGuard_ExclusiveAccessResourceGuard_specification
    {
        [Test] public void When_one_thread_has_UpdateLock_other_thread_is_blocked_until_first_thread_disposes_lock()
        {
            var resourceGuard = MonitorCE.WithTimeout(1.Seconds());

            var updateLock = resourceGuard.EnterNotifyAllLock();

            using var otherThreadIsWaitingForLock = new ManualResetEventSlim(false);
            using var otherThreadGotLock = new ManualResetEventSlim(false);
            var otherThreadTask = TaskCE.Run(() =>
            {
                otherThreadIsWaitingForLock.Set();
                using(resourceGuard.EnterNotifyAllLock())
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

        [Test] public void When_one_thread_calls_AwaitUpdateLock_twice_other_thread_is_blocked_until_first_thread_disposes_both_locks()
        {
            var resourceGuard = MonitorCE.WithTimeout(1.Seconds());

            var updateLock1 = resourceGuard.EnterNotifyAllLock();
            var updateLock2 = resourceGuard.EnterNotifyAllLock();

            using var otherThreadIsWaitingForLock = new ManualResetEventSlim(false);
            using var otherThreadGotLock = new ManualResetEventSlim(false);
            var otherThreadTask = TaskCE.Run("otherThreadTask",
                                             () =>
                                             {
                                                 otherThreadIsWaitingForLock.Set();
                                                 using(resourceGuard.EnterNotifyAllLock())
                                                 {
                                                     otherThreadGotLock.Set();
                                                 }
                                             });

            otherThreadIsWaitingForLock.Wait();
            otherThreadGotLock.Wait(10.Milliseconds()).Should().BeFalse();

            updateLock1.Dispose();
            otherThreadGotLock.Wait(10.Milliseconds()).Should().BeFalse();

            updateLock2.Dispose();
            otherThreadGotLock.Wait(10.Milliseconds()).Should().BeTrue();

            Task.WaitAll(otherThreadTask);
        }

        [TestFixture] public class Given_a_timeout_of_10_milliseconds_an_exception_is_thrown_By_Get_within_15_milliseconds_if_lock_is_not_acquired
        {
            [Test] public void Exception_is_ObjectLockTimedOutException()
                => RunScenario(0.Milliseconds()).Should().BeOfType<EnterLockTimeoutException>();

            [Test] public void If_owner_thread_blocks_for_less_than_stackTrace_timeout_Exception_contains_owning_threads_stack_trace()
                => RunScenario(30.Milliseconds()).Message.Should().Contain(nameof(DisposeOwningThreadLock));

            [Test] public void If_owner_thread_blocks_for_more_than_stacktrace_timeout__Exception_does_not_contain_owning_threads_stack_trace()
            {
                EnterLockTimeoutException.TestingOnlyRunWithAlternativeTimeToWaitForOwningThreadStacktrace(
                    10.Milliseconds(),
                    () => RunScenario(20.Milliseconds()).Message.Should().NotContain(nameof(DisposeOwningThreadLock)));
            }

            static void DisposeOwningThreadLock(IDisposable disposable) => disposable.Dispose();

            static Exception RunScenario(TimeSpan ownerThreadWaitTime)
            {
                var resourceGuard = MonitorCE.WithTimeout(10.Milliseconds());

                var updateLock = resourceGuard.EnterNotifyAllLock();

                var thrownException = Assert.Throws<AggregateException>(
                                                 () => TaskCE.Run(() => resourceGuard.EnterNotifyAllLock())
                                                             .Wait())
                                            .InnerExceptions.Single();

                TaskCE.Run($"Wait_And_{nameof(DisposeOwningThreadLock)}",
                           async () =>
                           {
                               await Task.Delay(ownerThreadWaitTime);
                               DisposeOwningThreadLock(updateLock);
                           }).Wait();

                return thrownException;
            }
        }
    }
}
