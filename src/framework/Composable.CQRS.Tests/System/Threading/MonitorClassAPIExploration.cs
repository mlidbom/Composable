using System;
using System.Threading;
using Composable.Testing.Threading;
using FluentAssertions;
using Xunit;

namespace Composable.Tests.System.Threading
{
    public class MonitorClassAPIExploration
    {
        [Fact] public void Wait_returns_after_timeout_even_without_pulse()
        {
            var guarded = new object();

            Monitor.Enter(guarded);
            Monitor.Wait(guarded, 1.Milliseconds())
                   .Should()
                   .BeFalse();
        }

        [Fact] void Wait_does_not_return_return_until_lock_is_available_to_reaquire_after_timeout()
        {
            var guarded = new object();

            var threadOneWaitsOnLockSection = GatedCodeSection.WithTimeout(5.Seconds()).Open();
            var threadTwoHasAquiredLockAndWishesToReleaseItGate = ThreadGate.CreateClosedWithTimeout(5.Seconds());

            var waitTimeout = 100.Milliseconds();

            bool waitSucceeded = false;
            using(var taskRunner = TestingTaskRunner.WithTimeout(1.Seconds()))
            {
                taskRunner.Run(() =>
                {
                    Monitor.Enter(guarded);
                    threadOneWaitsOnLockSection.Execute(() => waitSucceeded = Monitor.Wait(guarded, waitTimeout));
                });

                threadOneWaitsOnLockSection.EntranceGate.AwaitPassedThroughCountEqualTo(1);

                taskRunner.Run(() =>
                {
                    Monitor.Enter(guarded);
                    threadTwoHasAquiredLockAndWishesToReleaseItGate.AwaitPassthrough();
                    Monitor.Exit(guarded);
                });

                threadTwoHasAquiredLockAndWishesToReleaseItGate.AwaitQueueLengthEqualTo(1);

                threadOneWaitsOnLockSection.ExitGate
                                           .TryAwaitPassededThroughCountEqualTo(1, timeout: 200.Milliseconds())
                                           .Should().Be(false);

                threadTwoHasAquiredLockAndWishesToReleaseItGate.AwaitLetOneThreadPassthrough();

                threadOneWaitsOnLockSection.ExitGate.AwaitPassedThroughCountEqualTo(1);

                waitSucceeded.Should().Be(false);

            }
        }

        [Fact]
        void Wait_does_not_hang_on_long_timeout_values()
        {
            var guarded = new object();

            var threadOneWaitsOnLockSection = GatedCodeSection.WithTimeout(5.Seconds()).Open();
            var threadTwoHasAquiredLockAndWishesToReleaseItGate = ThreadGate.CreateClosedWithTimeout(5.Seconds());

            var waitTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

            bool waitSucceeded = false;
            using (var taskRunner = TestingTaskRunner.WithTimeout(1.Seconds()))
            {
                taskRunner.Run(() =>
                {
                    Monitor.Enter(guarded);
                    threadOneWaitsOnLockSection.Execute(() => waitSucceeded = Monitor.Wait(guarded, waitTimeout));
                });

                threadOneWaitsOnLockSection.EntranceGate.AwaitPassedThroughCountEqualTo(1);

                taskRunner.Run(() =>
                {
                    Monitor.Enter(guarded);
                    threadTwoHasAquiredLockAndWishesToReleaseItGate.AwaitPassthrough();
                    Monitor.PulseAll(guarded);
                    Monitor.Exit(guarded);
                });

                threadTwoHasAquiredLockAndWishesToReleaseItGate.AwaitQueueLengthEqualTo(1);

                threadOneWaitsOnLockSection.ExitGate
                                           .TryAwaitPassededThroughCountEqualTo(1, timeout: 200.Milliseconds())
                                           .Should().Be(false);

                threadTwoHasAquiredLockAndWishesToReleaseItGate.AwaitLetOneThreadPassthrough();

                threadOneWaitsOnLockSection.ExitGate.AwaitPassedThroughCountEqualTo(1);

                waitSucceeded.Should().Be(true);
            }
        }

        //[Fact] void MonitorHangs() => Test.Main();
    }
}
