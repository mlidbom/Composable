using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Composable.System.Threading
{
    static class TaskExtensions
    {
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
    }
}
