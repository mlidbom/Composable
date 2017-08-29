using System;
using System.Threading;
using Composable.System.Linq;
using Composable.Testing.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.Testing.Threading
{
    [TestFixture] public class Given_a_locked_ThreadGate
    {
        [Test] public void Calling_AllowOneThreadToPassThrough_twice_throws_an_ObjectTimeOutException_since_the_gate_is_open_due_to_the_first_call_and_no_threads_having_passed()
            => Assert.Throws<Composable.Contracts.AssertionException>(() => ThreadGate.WithTimeout(10.Milliseconds()).LetOneThreadPass().LetOneThreadPass());

        public class After_starting_10_threads_that_all_call_PassThrough
        {
            [Test] public void Within_10_milliseconds_all_threads_are_blocked_on_Passthrough_and_none_have_passed_the_gate()
            {
                var fixture = ThreadGateTestFixture.StartEntrantsOnThreads(10);
                fixture.Gate.Await(10.Milliseconds(), gate => gate.Queued == fixture.NumberOfThreads);
                fixture.ThreadsPassedTheGate(0.Milliseconds()).Should().Be(0);
            }

            public class And_all_have_queued_up_calling_PassThrough
            {
                ThreadGateTestFixture _fixture;

                [SetUp] public void Setup() { _fixture = ThreadGateTestFixture.StartEntrantsOnThreads(10).WaitForAllThreadsToQueueUpAtPassThrough(); }

                [TearDown] public void TearDownTask() { _fixture.Dispose(); }

                [Test] public void _10_milliseconds_later_no_thread_has_passed_the_gate() => _fixture.ThreadsPassedTheGate(10.Milliseconds()).Should().Be(0);

                [Test] public void PassedThrough_is_0() => _fixture.Gate.Passed.Should().Be(0);

                [Test] public void QueueLength_is_10() => _fixture.Gate.Queued.Should().Be(10);

                [Test] public void RequestCount_is_10() => _fixture.Gate.Requested.Should().Be(10);
            }
        }

        [TestFixture] public class After_10_threads_have_queued_up_at_PassThrough_and_LetOneThreadPassThrough_is_called_once
        {
            ThreadGateTestFixture _fixture;
            [SetUp] public void SetupTask()
            {
                _fixture = ThreadGateTestFixture.StartEntrantsOnThreads(10).WaitForAllThreadsToQueueUpAtPassThrough();
                _fixture.Gate.LetOneThreadPass();
            }

            [TearDown] public void TearDownTask() => _fixture.Dispose();

            [Test] public void _10_milliseconds_later_one_thread_has_passed_the_gate() => _fixture.ThreadsPassedTheGate(10.Milliseconds()).Should().Be(1);

            [Test] public void PassedThrough_is_1() => _fixture.Gate.Passed.Should().Be(1);

            [Test] public void QueueLength_is_9() => _fixture.Gate.Queued.Should().Be(9);

            [Test] public void RequestCount_is_10() => _fixture.Gate.Requested.Should().Be(10);
        }

        [TestFixture] public class After_10_threads_have_queued_up_at_PassThrough_and_LetOneThreadPassThrough_is_called_five_times
        {
            ThreadGateTestFixture _fixture;
            [SetUp] public void SetupTask()
            {
                _fixture = ThreadGateTestFixture.StartEntrantsOnThreads(10).WaitForAllThreadsToQueueUpAtPassThrough();
                1.Through(5).ForEach(_ => _fixture.Gate.LetOneThreadPass().AwaitClosed());
            }

            [TearDown] public void TearDownTask() => _fixture.Dispose();

            [Test] public void _10_milliseconds_later_five_threads_have_passed_the_gate() => _fixture.ThreadsPassedTheGate(10.Milliseconds()).Should().Be(5);

            [Test] public void PassedThrough_is_5() => _fixture.Gate.Passed.Should().Be(5);

            [Test] public void QueueLength_is_5() => _fixture.Gate.Queued.Should().Be(5);

            [Test] public void RequestCount_is_10() => _fixture.Gate.Requested.Should().Be(10);
        }

        [TestFixture(3, 1)]
        [TestFixture(7, 2)]
        [TestFixture(12, 3)]
        [TestFixture(10, 4)]
        [TestFixture(5, 5)]
        [TestFixture(10, 6)]
        [TestFixture(10, 7)]
        [TestFixture(10, 8)]
        [TestFixture(10, 9)]
        [TestFixture(10, 10)]
        public class After_Y_threads_have_queued_up_at_PassThrough_and_LetOneThreadPassThrough_is_called_X_times_where_X_is_at_most_Y
        {
            readonly int _threads;
            readonly int _timesToCallLetOneThreadPassThrough;

            static int _instances;

            ThreadGateTestFixture _fixture;
            public After_Y_threads_have_queued_up_at_PassThrough_and_LetOneThreadPassThrough_is_called_X_times_where_X_is_at_most_Y(int threads, int timesToCallLetOneThreadPassThrough)
            {
                _threads = threads;
                _timesToCallLetOneThreadPassThrough = timesToCallLetOneThreadPassThrough;
                var instances = Interlocked.Increment(ref _instances) + 1;
                Console.WriteLine(instances);
            }

            [SetUp] public void SetupTask()
            {
                _fixture = ThreadGateTestFixture.StartEntrantsOnThreads(_threads).WaitForAllThreadsToQueueUpAtPassThrough();
                1.Through(_timesToCallLetOneThreadPassThrough).ForEach(_ => _fixture.Gate.LetOneThreadPass().AwaitClosed());
            }

            [TearDown] public void TearDownTask() => _fixture.Dispose();

            [Test] public void _100_milliseconds_later_X_threads_have_passed_the_gate() => _fixture.ThreadsPassedTheGate(100.Milliseconds()).Should().Be(_timesToCallLetOneThreadPassThrough);

            [Test] public void PassedThrough_is_X() => _fixture.Gate.Passed.Should().Be(_timesToCallLetOneThreadPassThrough);

            [Test] public void QueueLength_is_Y_minus_X() => _fixture.Gate.Queued.Should().Be(Math.Max(0, _threads - _timesToCallLetOneThreadPassThrough));

            [Test] public void RequestCount_is_Y() => _fixture.Gate.Requested.Should().Be(_threads);
        }
    }
}
