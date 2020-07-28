using System;
using System.Threading.Tasks;
using Composable.GenericAbstractions;

namespace Composable.SystemCE.ThreadingCE.TasksCE
{
    class AsyncTaskCompletionSource<TResult>
    {
        readonly TaskCompletionSource<TResult> _completionSource;

        public AsyncTaskCompletionSource() => _completionSource = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<TResult> Task => _completionSource.Task;

        public void SetResultAsync(TResult result) => _completionSource.SetResult(result);
        public void SetExceptionAsync(Exception exception) => _completionSource.SetException(exception);
    }

    class AsyncTaskCompletionSource
    {
        readonly AsyncTaskCompletionSource<Unit> _completionSource;
        public Task Task => _completionSource.Task;

        public void SetResultAsync() => _completionSource.SetResultAsync(Unit.Instance);
        public void SetExceptionAsync(Exception exception) => _completionSource.SetExceptionAsync(exception);

        public AsyncTaskCompletionSource() => _completionSource = new AsyncTaskCompletionSource<Unit>();
    }
}
