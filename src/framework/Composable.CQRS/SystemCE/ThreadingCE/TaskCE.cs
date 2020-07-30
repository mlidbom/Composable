using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Composable.SystemCE.ThreadingCE
{
    static class TaskCE
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

        internal static Task ContinueAsynchronouslyOnDefaultScheduler(this Task @this, Action<Task> continuation, TaskContinuationOptions options = TaskContinuationOptions.RunContinuationsAsynchronously) => @this.ContinueWith(continuation, CancellationToken.None, options, TaskScheduler.Default);

        internal static Task ContinueAsynchronouslyOnDefaultScheduler<TResult>(this Task<TResult> @this, Action<Task<TResult>> continuation, TaskContinuationOptions options = TaskContinuationOptions.RunContinuationsAsynchronously) => @this.ContinueWith(continuation, CancellationToken.None, options, TaskScheduler.Default);

        public static Task StartOnDedicatedThread(Action action) => Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        ///<summary>Here just in order to collect all run methods here so that all task execution comes through this class and we can easily find them all and review them for why they don't pass a name for easier debugging. Any call to  <see cref="Task.Run(System.Action)"/> elsewhere is known to be a mistake to be replaced with a call to this method.</summary>
        public static Task Run(Action action) => Task.Run(action);

        ///<summary>Here just in order to collect all run methods here so that all task execution comes through this class and we can easily find them all and review them for why they don't pass a name for easier debugging. Any call to  <see cref="Task.Run(System.Action)"/> elsewhere is known to be a mistake to be replaced with a call to this method.</summary>
        public static Task<TResult> Run<TResult>(Func<TResult> function) => Task.Run(function);

        ///<summary>Using this instead of Task.Run can help debugging very much by providing a "name" for the task in the debugger views for Tasks</summary>
        public static Task Run(string name, Action action) => Run(name, CancellationToken.None, action);
        public static Task Run(string name, CancellationToken cancellationToken, Action action) => Task.Factory.StartNew(_ => action(), name, cancellationToken, TaskCreationOptions.None, TaskScheduler.Default);
    }
}
