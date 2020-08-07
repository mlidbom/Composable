using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Composable.SystemCE.ThreadingCE.TasksCE
{
    static partial class TaskCE
    {
        ///<summary>
        /// Abbreviated version of ConfigureAwait(continueOnCapturedContext: false)
        ///We need to apply this to every single await in library code for performance and to avoid deadlocks in clients with a synchronization context. It is not enough to have it at the edges of the public API: https://tinyurl.com/y8cjr77w, https://tinyurl.com/vwrcd8j, https://tinyurl.com/y7sxqb53, https://tinyurl.com/n6zheop,
        ///This is important for performance because when called from a UI thread with a Synchronization context capturing it for every await causes performance problems by a thousand paper cuts: https://tinyurl.com/vwrcd8j
        ///Hacks that null out the SynchronizationContext are not reliable because await uses TaskScheduler.Current so no SetSynchronizationContext hack will work reliably.  https://tinyurl.com/y37d54xg
        ///</summary>
        internal static ConfiguredTaskAwaitable NoMarshalling(this Task @this) => @this.ConfigureAwait(continueOnCapturedContext: false);

        ///<summary>
        /// Abbreviated version of ConfigureAwait(continueOnCapturedContext: false)
        ///We need to apply this to every single await in library code for performance and to avoid deadlocks in clients with a synchronization context. It is not enough to have it at the edges of the public API: https://tinyurl.com/y8cjr77w, https://tinyurl.com/vwrcd8j, https://tinyurl.com/y7sxqb53, https://tinyurl.com/n6zheop,
        ///This is important for performance because when called from a UI thread with a Synchronization context capturing it for every await causes performance problems by a thousand paper cuts: https://tinyurl.com/vwrcd8j
        ///Hacks that null out the SynchronizationContext are not reliable because await uses TaskScheduler.Current so no SetSynchronizationContext hack will work reliably.  https://tinyurl.com/y37d54xg
        ///</summary>
        internal static ConfiguredTaskAwaitable<TResult> NoMarshalling<TResult>(this Task<TResult> @this) => @this.ConfigureAwait(continueOnCapturedContext: false);

        //Todo: Consider making it policy to start all public async methods by calling this method. This would remove the need to call NoMarshalling for every single await in the codebase.
        ///<summary>Guarantees that after the returned Task has been awaited <see cref="SynchronizationContext.Current"/> == null and <see cref="TaskScheduler.Current"/> == <see cref="TaskScheduler.Default"/>
        ///Thus no deadlocks will occur even if user code waits synchronously on this task and some code that this task calls does not use ConfigureAwait(false)
        /// </summary>
        internal static GuaranteeContinuationRunsAsyncOnDefaultSchedulerWithoutSynchronizationContextAwaiter ResetSynchronizationContextAndScheduler() =>
            Task.CompletedTask.ResetSynchronizationContextAndScheduler();

        internal static GuaranteeContinuationRunsAsyncOnDefaultSchedulerWithoutSynchronizationContextAwaiter ResetSynchronizationContextAndScheduler(this Task task) =>
            new GuaranteeContinuationRunsAsyncOnDefaultSchedulerWithoutSynchronizationContextAwaiter(task);

        internal readonly struct GuaranteeContinuationRunsAsyncOnDefaultSchedulerWithoutSynchronizationContextAwaiter : ICriticalNotifyCompletion
        {
            readonly Task _task;
            internal GuaranteeContinuationRunsAsyncOnDefaultSchedulerWithoutSynchronizationContextAwaiter(Task task) => _task = task;

            // ReSharper disable UnusedMember.Global called by .Net runtime infrastructure.
            public GuaranteeContinuationRunsAsyncOnDefaultSchedulerWithoutSynchronizationContextAwaiter GetAwaiter() => this;

            public bool IsCompleted
            {
                get
                {
                    if(SynchronizationContext.Current != null || TaskScheduler.Current != TaskScheduler.Default)
                    {
                        return false;
                    } else
                    {
                        return _task.IsCompleted;
                    }
                }
            }

            ///<summary>Delegates to the Task's own awaiter</summary>
            public void GetResult() => _task.GetAwaiter().GetResult();
            // ReSharper restore UnusedMember.Global

            ///<summary>Forwards request to the Task, but uses <see cref="TaskCE.NoMarshalling"/> to get rid of any <see cref="SynchronizationContext.Current"/></summary>
            public void OnCompleted(Action action) => _task.NoMarshalling().GetAwaiter().OnCompleted(action);

            ///<summary>Forwards request to the Task, but uses <see cref="TaskCE.NoMarshalling"/> to get rid of any <see cref="SynchronizationContext.Current"/></summary>
            public void UnsafeOnCompleted(Action action) => _task.NoMarshalling().GetAwaiter().UnsafeOnCompleted(action);
        }
    }
}
