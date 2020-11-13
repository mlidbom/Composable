using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable MethodSupportsCancellation

namespace Composable.SystemCE.ThreadingCE.TasksCE
{
    static partial class TaskCE
    {
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

        //bug: This method is used by sync code in a way that does not await the task returned.
        internal static Task ContinueAsynchronouslyOnDefaultScheduler(this Task @this, Action<Task> continuation, TaskContinuationOptions options = TaskContinuationOptions.RunContinuationsAsynchronously) =>
            @this.ContinueWith(continuation, CancellationToken.None, options, TaskScheduler.Default);

        //bug: This method is used by sync code in a way that does not await the task returned.
        internal static Task ContinueAsynchronouslyOnDefaultScheduler<TResult>(this Task<TResult> @this, Action<Task<TResult>> continuation, TaskContinuationOptions options = TaskContinuationOptions.RunContinuationsAsynchronously) =>
            @this.ContinueWith(continuation, CancellationToken.None, options, TaskScheduler.Default);
    }
}
