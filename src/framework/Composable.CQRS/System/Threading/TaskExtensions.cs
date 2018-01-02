using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Composable.Contracts;

namespace Composable.System.Threading
{
    static class TaskExtensions
    {
        internal static ConfiguredTaskAwaitable NoMarshalling(this Task @this) => @this.ConfigureAwait(continueOnCapturedContext: false);

        internal static ConfiguredTaskAwaitable<TResult> NoMarshalling<TResult>(this Task<TResult> @this) => @this.ConfigureAwait(continueOnCapturedContext: false);

        internal static async Task<TResult> Cast<TSource, TResult>(this Task<TSource> @this)
        {
            var typedCompletionSource = new TaskCompletionSource<TResult>();

#pragma warning disable 4014
            @this.ContinueWith(result =>
#pragma warning restore 4014
                               {
                                   if(result.IsCanceled)
                                   {
                                       typedCompletionSource.SetCanceled();
                                   } else if(result.IsFaulted)
                                   {
                                       typedCompletionSource.SetException(result.Exception.InnerExceptions.Single());
                                   }

                                   Contract.Result.Assert(result.IsCompleted);
                                   try
                                   {
                                       typedCompletionSource.SetResult((TResult)(object)result.Result);
                                   }
                                   catch(Exception exception)
                                   {
                                       typedCompletionSource.SetException(exception);
                                   }
                               });

            return await typedCompletionSource.Task;
        }
    }
}
