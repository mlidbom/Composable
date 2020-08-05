using System;
using System.Threading.Tasks;
using Composable.Contracts;

namespace Composable.SystemCE.ThreadingCE
{
    enum SyncOrAsync
    {
        Async,
        Sync
    }

    static class SyncOrAsyncCE
    {
        internal static Func<TParam, Task<TResult>> AsAsync<TParam, TResult>(this Func<TParam, TResult> func) =>
            param => Task.FromResult(func(param));

        internal static Action AsSync(this Func<SyncOrAsync, Task> func) =>
            () => func(SyncOrAsync.Sync).SyncResult();

        internal static Func<Task<TResult>> AsAsync<TResult>(this Func<TResult> func) =>
            () => Task.FromResult(func());

        internal static Func<Task> AsAsync(this Func<SyncOrAsync, Task> func) =>
            async () => await func(SyncOrAsync.Async).NoMarshalling();



        internal static async Task Run(this SyncOrAsync syncOrAsync, Action synchronous, Func<Task> asynchronous)
        {
            if(syncOrAsync == SyncOrAsync.Async)
            {
                await asynchronous().NoMarshalling();
            } else
            {
                synchronous();
            }
        }

        internal static TResult SyncResult<TResult>(this Task<TResult> @this)
        {
            //Should only ever be called when in the sync mode, so assert that the task is done.
            Assert.Argument.Assert(@this.IsCompleted);
            return @this.GetAwaiter().GetResult();
        }

        internal static void SyncResult(this Task @this)
        {
            //Should only ever be called when in the sync mode, so assert that the task is done.
            Assert.Argument.Assert(@this.IsCompleted);
            @this.GetAwaiter().GetResult();
        }
    }
}