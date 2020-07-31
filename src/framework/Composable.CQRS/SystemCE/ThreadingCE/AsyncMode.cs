using System;
using System.Threading.Tasks;

namespace Composable.SystemCE.ThreadingCE
{
    enum AsyncMode
    {
        Async,
        Sync
    }

    static class AsyncCe
    {
        internal static Func<TParam, Task<TResult>> AsAsync<TParam, TResult>(this Func<TParam, TResult> func) =>
            param =>
            {
                var result = func(param);
                return Task.FromResult(result);
            };

        internal static Func<TParam, Task> AsAsync<TParam>(this Action<TParam> func) =>
            param =>
            {
                func(param);
                return Task.CompletedTask;
            };

        internal static async Task<TResult> Run<TResult>(this AsyncMode mode, Func<AsyncMode, Task<TResult>> asynchronous)
        {
            if(mode == AsyncMode.Async)
            {
                return await asynchronous(mode).NoMarshalling();
            } else
            {
                return asynchronous(mode).AwaiterResult();
            }
        }

        internal static async Task<TResult> Run<TResult>(this AsyncMode mode, Func<TResult> synchronous, Func<Task<TResult>> asynchronous)
        {
            if(mode == AsyncMode.Async)
            {
                return await asynchronous().NoMarshalling();
            } else
            {
                return synchronous();
            }
        }

        internal static async Task Run(this AsyncMode mode, Action synchronous, Func<Task> asynchronous)
        {
            if(mode == AsyncMode.Async)
            {
                await asynchronous().NoMarshalling();
            } else
            {
                synchronous();
            }
        }
    }
}