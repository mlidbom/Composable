using System.Threading;
using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE.TasksCE;
using FluentAssertions;
using Nito.AsyncEx;
using NUnit.Framework;

namespace Composable.Tests.SystemCE.ThreadingCE.TasksCE
{
    public class TaskCE_SynchronizationContext_Tests
    {
        [Test] public async Task ResetSynchronizationContextAndScheduler_called_with_no_context_or_custom_Scheduler_returns_synchronously()
        {
            var originalThread = Thread.CurrentThread.ManagedThreadId;
            await TaskCE.ResetSynchronizationContextAndScheduler();
            Thread.CurrentThread.ManagedThreadId.Should().Be(originalThread);
        }

        [Test] public void ResetSynchronizationContextAndScheduler_called_with_context_returns_asynchronously() => AsyncContext.Run(async () =>
        {
            var originalThread = Thread.CurrentThread.ManagedThreadId;
            await TaskCE.ResetSynchronizationContextAndScheduler();
            Thread.CurrentThread.ManagedThreadId.Should().NotBe(originalThread);
        });

        [Test] public void ResetSynchronizationContextAndScheduler_called_with_context_removes_context() => AsyncContext.Run(async () =>
        {
            SynchronizationContext.Current.Should().NotBeNull();
            await TaskCE.ResetSynchronizationContextAndScheduler();
            SynchronizationContext.Current.Should().BeNull();
        });


        [Test] public async Task ResetSynchronizationContextAndScheduler_called_with_custom_scheduler_removes_scheduler()
        {
            var concurrentExclusiveSchedulerPair = new ConcurrentExclusiveSchedulerPair();
            await await Task.Factory.StartNew(
                      async () =>
                      {
                          TaskScheduler.Current.Should().NotBe(TaskScheduler.Default);
                          await TaskCE.ResetSynchronizationContextAndScheduler();
                          TaskScheduler.Current.Should().Be(TaskScheduler.Default);
                      },
                      CancellationToken.None,
                      TaskCreationOptions.None,
                      concurrentExclusiveSchedulerPair.ConcurrentScheduler);
        }

        [Test] public async Task ResetSynchronizationContextAndScheduler_called_with_context_custom_scheduler_removes_both_context_and_scheduler() =>
            await AsyncContext.Run(
                async () => await Task.Factory.StartNew(
                                async () =>
                                {
                                    SynchronizationContext.Current.Should().NotBeNull();
                                    TaskScheduler.Current.Should().NotBe(TaskScheduler.Default);
                                    await TaskCE.ResetSynchronizationContextAndScheduler();
                                    SynchronizationContext.Current.Should().BeNull();
                                    TaskScheduler.Current.Should().Be(TaskScheduler.Default);
                                },
                                CancellationToken.None,
                                TaskCreationOptions.None,
                                TaskScheduler.FromCurrentSynchronizationContext()));
    }
}
