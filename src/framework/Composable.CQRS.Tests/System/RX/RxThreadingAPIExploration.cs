using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using Composable.System.Threading.ResourceAccess;
using Composable.Testing.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.System.RX
{
    using Composable.System;

    [TestFixture]
    public class RxThreadingApiExploration
    {
        public class SubjectExploration
        {
            TestingTaskRunner _taskRunner;
            [SetUp] public void SetupTask() => _taskRunner = new TestingTaskRunner(20.Seconds());
            [TearDown] public void TearDownTask() => _taskRunner.Dispose();



            [Test] public void By_default_calling_OnNext_from_multiple_threads_executes_single_subscriber_on_multiple_threads()
            {
                var subject = new Subject<string>();

                var subscriberGate = ThreadGate.CreateClosedWithTimeout(1.Seconds());

                using(subject.Subscribe(value => subscriberGate.AwaitPassthrough()))
                {
                    _taskRunner.Run(() => subject.OnNext("1"), () => subject.OnNext("2"));

                    subscriberGate.AwaitQueueLengthEqualTo(2);

                    subscriberGate.Open();

                    _taskRunner.WaitForTasksToComplete();

                    subscriberGate.PassedThrough[0].Thread.ManagedThreadId
                                  .Should().NotBe(subscriberGate.PassedThrough[1].Thread.ManagedThreadId);

                }
            }

            [Test]
            public void After_calling_Synchronize_calling_OnNext_from_multiple_threads_executes_single_subscriber_on_multiple_thread_but_one_at_a_time()
            {
                var subject = new Subject<string>();
                var synchronized = subject.Synchronize();

                var subscriberGate = ThreadGate.CreateClosedWithTimeout(2.Seconds());

                using (synchronized.Subscribe(value => subscriberGate.AwaitPassthrough()))
                {
                    _taskRunner.Run(() => subject.OnNext("1"), () => subject.OnNext("2"));

                    subscriberGate.AwaitQueueLengthEqualTo(1);
                    AssertionExtensions.ShouldThrow<Exception>(() => subscriberGate.AwaitQueueLengthEqualTo(2, 0.1.Seconds()));
                    subscriberGate.Open();

                    _taskRunner.WaitForTasksToComplete();

                    subscriberGate.PassedThrough[0].Thread.ManagedThreadId
                                  .Should().NotBe(subscriberGate.PassedThrough[1].Thread.ManagedThreadId);

                }
            }

            //[Test] public void After_calling_Synchronize_calling_OnNext_from_multiple_threads_executes_multiple_subscribers_on_multiple_thread_but_one_at_a_time()
            //{
            //    var subject = new Subject<string>();
            //    var synchronized = subject.Synchronize();

            //    var subscriberGate = ThreadGate.CreateClosedWithTimeout(5.Seconds());

            //    var subscriberCount = 5;
            //    var onNextCalls = 2;

            //    var subscribers = 1.Through(subscriberCount).Select(index => synchronized.Subscribe(value => subscriberGate.AwaitPassthrough())).ToList();

            //    _taskRunner.Run(1.Through(onNextCalls).Select(index => (Action)(() => subject.OnNext(index.ToString()))));

            //    for(int currentCallback = 1; currentCallback <= subscriberCount * onNextCalls; currentCallback++)
            //    {
            //        subscriberGate.AwaitQueueLengthEqualTo(2);
            //        AssertionExtensions.ShouldThrow<Exception>(() => subscriberGate.AwaitQueueLengthEqualTo(2, 0.1.Seconds()));
            //        subscriberGate.AwaitLetOneThreadPassthrough();
            //    }

            //    _taskRunner.WaitForTasksToComplete();

            //    subscriberGate.PassedThrough[0].Thread.ManagedThreadId
            //                  .Should().NotBe(subscriberGate.PassedThrough[1].Thread.ManagedThreadId);


            //    subscribers.ForEach(@this => @this.Dispose());
            //}

            [Test]
            public void After_calling_Synchronize_calling_OnNext_from_multiple_threads_executes_multiple_subscribers_on_multiple_threads_but_never_calls_the_same_subscriber_in_parallel()
            {
                var subject = new Subject<int>();
                var synchronized = subject.Synchronize();

                const int subscriberCount = 3;
                const int onNextCalls = 5;

                var concurrentCalls = 0;
                var maxConcurrentCalls = 0;
                var subscriberConcurrentCalls = new int[subscriberCount];
                var maxConcurrentCallsPerSubscriber = new int[subscriberCount];
                var guard = ResourceGuard.WithTimeout(1.Seconds());
                var subscribers = 0.Through(subscriberCount - 1).Select(index => synchronized.Subscribe(
                                                                        value =>
                                                                        {
                                                                            guard.Update(() =>
                                                                            {
                                                                                Console.WriteLine(++concurrentCalls);
                                                                                subscriberConcurrentCalls[index]++;
                                                                                maxConcurrentCallsPerSubscriber[index] = Math.Max(subscriberConcurrentCalls[index], maxConcurrentCallsPerSubscriber[index]);
                                                                                maxConcurrentCalls = Math.Max(concurrentCalls, maxConcurrentCalls);
                                                                            });
                                                                            Thread.Sleep(10);
                                                                            guard.Update(() =>
                                                                            {
                                                                                concurrentCalls--;
                                                                                subscriberConcurrentCalls[index]--;
                                                                            });
                                                                        })).ToList();

                _taskRunner.Run(1.Through(onNextCalls).Select(index => (Action)(() => subject.OnNext(index))))
                           .WaitForTasksToComplete();

                Console.WriteLine($"{nameof(maxConcurrentCalls)}: {maxConcurrentCalls}");
                maxConcurrentCalls.Should().Be(Math.Min(subscriberCount, onNextCalls));

                maxConcurrentCallsPerSubscriber.ForEach(maxConcurrentCallsForThisSubscriber => maxConcurrentCallsForThisSubscriber.Should().Be(1));

                subscribers.ForEach(@this => @this.Dispose());
            }

            [Test]
            public void After_calling_Synchronize_and_ObserveOn_DefaultScheduler_Instance_calling_OnNext_from_multiple_threads_executes_multiple_subscribers_on_multiple_threads_but_never_calls_the_same_subscriber_in_parallel()
            {
                var subject = new Subject<int>();
                var synchronized = subject.Synchronize().ObserveOn(Scheduler.Default);
                var subscriberThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());

                const int subscriberCount = 3;
                const int onNextCalls = 5;

                var concurrentCalls = 0;
                var maxConcurrentCalls = 0;
                var subscriberConcurrentCalls = new int[subscriberCount];
                var maxConcurrentCallsPerSubscriber = new int[subscriberCount];
                var guard = ResourceGuard.WithTimeout(1.Seconds());
                var subscribers = 0.Through(subscriberCount - 1).Select(index => synchronized.Subscribe(
                                                                        value =>
                                                                        {
                                                                            guard.Update(() =>
                                                                            {
                                                                                ++concurrentCalls;
                                                                                subscriberConcurrentCalls[index]++;
                                                                                maxConcurrentCallsPerSubscriber[index] = Math.Max(subscriberConcurrentCalls[index], maxConcurrentCallsPerSubscriber[index]);
                                                                                maxConcurrentCalls = Math.Max(concurrentCalls, maxConcurrentCalls);
                                                                            });
                                                                            Thread.Sleep(10);
                                                                            subscriberThreadGate.AwaitPassthrough();
                                                                            guard.Update(() =>
                                                                            {
                                                                                --concurrentCalls;
                                                                                subscriberConcurrentCalls[index]--;
                                                                            });
                                                                        })).ToList();

                _taskRunner.Run(1.Through(onNextCalls).Select(index => (Action)(() => subject.OnNext(index))));

                _taskRunner.WaitForTasksToComplete();
                subscriberThreadGate.AwaitPassedThroughCountEqualTo(subscriberCount * onNextCalls);

                Console.WriteLine($"{nameof(maxConcurrentCalls)}: {maxConcurrentCalls}");
                maxConcurrentCalls.Should().Be(subscriberCount);
                concurrentCalls.Should().Be(0);

                maxConcurrentCallsPerSubscriber.ForEach(maxConcurrentCallsForThisSubscriber => maxConcurrentCallsForThisSubscriber.Should().Be(1));

                subscribers.ForEach(@this => @this.Dispose());
            }

            [Test]
            public void After_calling_Synchronize_and_ObserveOn_using_DefaultScheduler_Instance_calling_OnNext_from_multiple_threads_executes_subscribers_on_single_thread()
            {
                var subject = new Subject<string>();
                var synchronized = subject.ObserveOn(Scheduler.Default);

                var subscriberGate = ThreadGate.CreateClosedWithTimeout(2.Seconds());

                using (synchronized.Subscribe(value => subscriberGate.AwaitPassthrough()))
                {
                    _taskRunner.Run(1.Through(100).Select(index => (Action)(() => subject.OnNext(index.ToString()))));

                    subscriberGate.AwaitQueueLengthEqualTo(1);
                    AssertionExtensions.ShouldThrow<Exception>(() => subscriberGate.AwaitQueueLengthEqualTo(2, 0.1.Seconds()));
                    subscriberGate.Open();

                    _taskRunner.WaitForTasksToComplete();
                    subscriberGate.AwaitPassedThroughCountEqualTo(100);

                    var firstThreadId = subscriberGate.PassedThrough[0].Thread.ManagedThreadId;

                    int current = 0;
                    foreach(var threadSnapshot in subscriberGate.PassedThrough)
                    {
                        Console.WriteLine($"{current++:D2}:{threadSnapshot.Thread.ManagedThreadId}");
                        threadSnapshot.Thread.ManagedThreadId.Should().Be(firstThreadId);
                    }

                }
            }
        }
    }
}
