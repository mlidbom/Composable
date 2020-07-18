using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Composable.System.Threading
{
    static class TaskExtensions
    {
        //We need to apply this to every single await for performance and to avoid deadlocks in clients with a synchronization context. It is not enough to have it at the edges of the public API: https://tinyurl.com/y8cjr77w, https://tinyurl.com/vwrcd8j, https://tinyurl.com/y7sxqb53, https://tinyurl.com/n6zheop,
        //Also hacks that null out the SyncronizationContext are not reliable because await uses TaskScheduler.Current so no SetSynchronizationContext hack will do the trick reliably.  https://tinyurl.com/y37d54xg
        internal static ConfiguredTaskAwaitable NoMarshalling(this Task @this) => @this.ConfigureAwait(continueOnCapturedContext: false);
        internal static ConfiguredTaskAwaitable<TResult> NoMarshalling<TResult>(this Task<TResult> @this) => @this.ConfigureAwait(continueOnCapturedContext: false);

        internal static TResult ResultUnwrappingException<TResult>(this Task<TResult> task)
        {
            try
            {
                return task.Result;
            }
            catch(AggregateException exception)
            {
                if(exception.InnerExceptions.Count == 1 && exception.InnerException != null)
                {
                    ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                } else
                {
                    throw;
                }
            }

            throw new Exception("Impossible!");
        }

        internal static void WaitUnwrappingException(this Task task)
        {
            try
            {
                task.Wait();
                return;
            }
            catch(AggregateException exception)
            {
                if(exception.InnerExceptions.Count == 1 && exception.InnerException != null)
                {
                    ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                } else
                {
                    throw;
                }
            }

            throw new Exception("Impossible!");
        }

        internal static Task ContinueOnDefaultScheduler(this Task @this, Action<Task> continuation, TaskContinuationOptions options = TaskContinuationOptions.None) => @this.ContinueWith(continuation, CancellationToken.None, options, TaskScheduler.Default);

        public static Task StartLongRunning(Action action) => Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
}
