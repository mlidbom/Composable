using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Composable.System.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.Testing.Threading
{
    [TestFixture] public class ObjectLock_specification
    {
        [Test] public void When_one_thread_is_running_action_version_of_execute_other_thread_is_blocked_until_first_thread_exits_execute()
        {
            var lockedObject = new object();

            var firstThreadHasEnteredExecute = new ManualResetEventSlim(false);
            var allowFirstThreadToExitExecute = new ManualResetEventSlim(false);
            var firstThreadTask = Task.Run(
                () => ObjectLock.Execute(
                    lockedObject,
                    2.Seconds(),
                    () =>
                    {
                        firstThreadHasEnteredExecute.Set();
                        allowFirstThreadToExitExecute.Wait();
                    }));

            firstThreadHasEnteredExecute.Wait();

            var otherThreadIsWaitingForLock = new ManualResetEventSlim(false);
            var otherThreadGotLock = new ManualResetEventSlim(false);
            var otherThreadTask = Task.Run(
                () =>
                {
                    Task.Run(() => otherThreadIsWaitingForLock.Set());
                    ObjectLock.Get(lockedObject, 1.Seconds());
                    otherThreadGotLock.Set();
                });

            otherThreadIsWaitingForLock.Wait();

            otherThreadGotLock.Wait(10.Milliseconds()).Should()
                              .BeFalse();

            allowFirstThreadToExitExecute.Set();

            otherThreadGotLock.Wait(10.Milliseconds())
                              .Should()
                              .BeTrue();

            Task.WaitAll(firstThreadTask, otherThreadTask);
        }

        [Test]
        public void When_one_thread_is_running_func_version_of_execute_other_thread_is_blocked_until_first_thread_exits_execute()
        {
            var lockedObject = new object();

            var firstThreadHasEnteredExecute = new ManualResetEventSlim(false);
            var allowFirstThreadToExitExecute = new ManualResetEventSlim(false);
            var firstThreadTask = Task.Run(
                () =>
                {
                    ObjectLock.Execute(
                        lockedObject,
                        2.Seconds(),
                        () =>
                        {
                            firstThreadHasEnteredExecute.Set();
                            allowFirstThreadToExitExecute.Wait();
                            return new object();
                        });
                });

            firstThreadHasEnteredExecute.Wait();

            var otherThreadIsWaitingForLock = new ManualResetEventSlim(false);
            var otherThreadGotLock = new ManualResetEventSlim(false);
            var otherThreadTask = Task.Run(
                () =>
                {
                    Task.Run(() => otherThreadIsWaitingForLock.Set());
                    ObjectLock.Get(lockedObject, 1.Seconds());
                    otherThreadGotLock.Set();
                });

            otherThreadIsWaitingForLock.Wait();

            otherThreadGotLock.Wait(10.Milliseconds()).Should()
                              .BeFalse();

            allowFirstThreadToExitExecute.Set();

            otherThreadGotLock.Wait(10.Milliseconds())
                              .Should()
                              .BeTrue();

            Task.WaitAll(firstThreadTask, otherThreadTask);
        }

        [TestFixture] public class Given_a_timeout_of_10_milliseconds_an_exception_is_thrown_By_Get_within_15_milliseconds_if_lock_is_not_acquired
        {
            [Test] public void Exception_is_ObjectLockTimedOutException()
            {
                RunScenario(ownerThreadWaitTime: 0.Milliseconds())
                    .Should()
                    .BeOfType<ObjectLockTimedOutException>();
            }

            [Test] public void If_owner_thread_blocks_for_less_than_stacktrace_timeout_Exception_contain_owning_threads_stack_trace()
            {
                ObjectLockTimedOutException.TestingOnlyRunWithModifiedTimeToWaitForOwningThreadStacktrace(
                    50.Milliseconds(),
                    () => RunScenario(ownerThreadWaitTime: 30.Milliseconds())
                        .Message.Should()
                        .Contain(nameof(DisposeOwningThreadLock)));
            }

            [Test] public void If_owner_thread_blocks_for_more_than_stacktrace_timeout__Exception_does_not_contain_owning_threads_stack_trace()
            {
                ObjectLockTimedOutException.TestingOnlyRunWithModifiedTimeToWaitForOwningThreadStacktrace(
                    50.Milliseconds(),
                    () => RunScenario(ownerThreadWaitTime: 100.Milliseconds())
                        .Message.Should()
                        .Contain(nameof(DisposeOwningThreadLock)));
            }

            [Test] public void PrintException() { Console.WriteLine(RunScenario(0.Milliseconds())); }

            static void DisposeOwningThreadLock(IDisposable disposable) { disposable.Dispose(); }

            Exception RunScenario(TimeSpan ownerThreadWaitTime)
            {
                var lockedObject = new object();
                var objectLock = ObjectLock.Get(lockedObject, 1.Milliseconds());

                var thrownException = Assert.Throws<AggregateException>(
                                                () => Task.Run(() => ObjectLock.Get(lockedObject, 15.Milliseconds()))
                                                          .Wait())
                                            .InnerExceptions.Single();

                Task.Run(
                    () =>
                    {
                        Thread.Sleep(ownerThreadWaitTime);
                        DisposeOwningThreadLock(objectLock);
                    });

                return thrownException;
            }
        }
    }
}
