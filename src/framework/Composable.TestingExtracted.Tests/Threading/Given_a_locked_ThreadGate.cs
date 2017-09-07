using System;
using System.Threading;
using Composable.System.Linq;
using Composable.System.Threading.ResourceAccess;
using Composable.Testing.Threading;
using FluentAssertions;
using Xunit;

namespace Composable.Testing.Tests.Threading
{
    public class Given_a_locked_ThreadGate
    {
        [Fact] public void Calling_AllowOneThreadToPassThrough_throws_an_AwaitingConditionTimedOutException_since_no_threads_are_waiting_to_pass()
            => Assert.Throws<AwaitingConditionTimedOutException>(() => ThreadGate.WithTimeout(10.Milliseconds()).LetOneThreadPass());

        public class After_starting_10_threads_that_all_call_PassThrough
        {
            [Fact] public void Within_10_milliseconds_all_threads_are_blocked_on_Passthrough_and_none_have_passed_the_gate()
            {
                var fixture = ThreadGateTestFixture.StartEntrantsOnThreads(10);
                fixture.Gate.Await(10.Milliseconds(), () => fixture.Gate.Queued == fixture.NumberOfThreads);
                fixture.ThreadsPassedTheGate(0.Milliseconds()).Should().Be(0);
            }

            public class And_all_have_queued_up_calling_PassThrough : IDisposable
            {
                ThreadGateTestFixture _fixture;

                public And_all_have_queued_up_calling_PassThrough() { _fixture = ThreadGateTestFixture.StartEntrantsOnThreads(10).WaitForAllThreadsToQueueUpAtPassThrough(); }

                public void Dispose() { _fixture.Dispose(); }

                [Fact] public void _10_milliseconds_later_no_thread_has_passed_the_gate() => _fixture.ThreadsPassedTheGate(10.Milliseconds()).Should().Be(0);

                [Fact] public void PassedThrough_is_0() => _fixture.Gate.Passed.Should().Be(0);

                [Fact] public void QueueLength_is_10() => _fixture.Gate.Queued.Should().Be(10);

                [Fact] public void RequestCount_is_10() => _fixture.Gate.Requested.Should().Be(10);
            }
        }

        public class After_10_threads_have_queued_up_at_PassThrough_and_LetOneThreadPassThrough_is_called_once : IDisposable
        {
            ThreadGateTestFixture _fixture;
            public After_10_threads_have_queued_up_at_PassThrough_and_LetOneThreadPassThrough_is_called_once()
            {
                _fixture = ThreadGateTestFixture.StartEntrantsOnThreads(10).WaitForAllThreadsToQueueUpAtPassThrough();
                _fixture.Gate.LetOneThreadPass();
            }

            public void Dispose() => _fixture.Dispose();

            [Fact] public void _10_milliseconds_later_one_thread_has_passed_the_gate() => _fixture.ThreadsPassedTheGate(10.Milliseconds()).Should().Be(1);

            [Fact] public void PassedThrough_is_1() => _fixture.Gate.Passed.Should().Be(1);

            [Fact] public void QueueLength_is_9() => _fixture.Gate.Queued.Should().Be(9);

            [Fact] public void RequestCount_is_10() => _fixture.Gate.Requested.Should().Be(10);
        }

        public class After_10_threads_have_queued_up_at_PassThrough_and_LetOneThreadPassThrough_is_called_five_times : IDisposable
        {
            ThreadGateTestFixture _fixture;
            public After_10_threads_have_queued_up_at_PassThrough_and_LetOneThreadPassThrough_is_called_five_times()
            {
                _fixture = ThreadGateTestFixture.StartEntrantsOnThreads(10).WaitForAllThreadsToQueueUpAtPassThrough();
                1.Through(5).ForEach(_ => _fixture.Gate.LetOneThreadPass().AwaitClosed());
            }

            public void Dispose() => _fixture.Dispose();

            [Fact] public void _10_milliseconds_later_five_threads_have_passed_the_gate() => _fixture.ThreadsPassedTheGate(10.Milliseconds()).Should().Be(5);

            [Fact] public void PassedThrough_is_5() => _fixture.Gate.Passed.Should().Be(5);

            [Fact] public void QueueLength_is_5() => _fixture.Gate.Queued.Should().Be(5);

            [Fact] public void RequestCount_is_10() => _fixture.Gate.Requested.Should().Be(10);
        }

        public class After_10_threads_have_queued_up_at_PassThrough_and_LetOneThreadPassThrough_is_called_10_times : IDisposable
        {
            ThreadGateTestFixture _fixture;
            public After_10_threads_have_queued_up_at_PassThrough_and_LetOneThreadPassThrough_is_called_10_times()
            {
                _fixture = ThreadGateTestFixture.StartEntrantsOnThreads(10).WaitForAllThreadsToQueueUpAtPassThrough();
                1.Through(10).ForEach(_ => _fixture.Gate.LetOneThreadPass().AwaitClosed());
            }

            public void Dispose() => _fixture.Dispose();

            [Fact] public void _10_milliseconds_later_10_threads_have_passed_the_gate() => _fixture.ThreadsPassedTheGate(10.Milliseconds()).Should().Be(10);

            [Fact] public void PassedThrough_is_5() => _fixture.Gate.Passed.Should().Be(10);

            [Fact] public void QueueLength_is_5() => _fixture.Gate.Queued.Should().Be(0);

            [Fact] public void RequestCount_is_10() => _fixture.Gate.Requested.Should().Be(10);

            [Fact] void Calling_LetOneThreadPassThroughAgainThrowsAnException() => Assert.ThrowsAny<Exception>(() => _fixture.Gate.LetOneThreadPass());
        }
    }
}
