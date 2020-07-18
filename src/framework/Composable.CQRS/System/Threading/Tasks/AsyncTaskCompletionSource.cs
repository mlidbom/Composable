using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.GenericAbstractions;
using Composable.System.Reflection;

namespace Composable.System.Threading.Tasks
{
    class AsyncTaskCompletionSource<TResult> : TaskCompletionSource<TResult>
    {
        public AsyncTaskCompletionSource() : base(TaskCreationOptions.RunContinuationsAsynchronously) {}
        public AsyncTaskCompletionSource(TaskCreationOptions creationOptions) : base(creationOptions | TaskCreationOptions.RunContinuationsAsynchronously) {}
    }

    class AsyncTaskCompletionSource
    {
        readonly TaskCompletionSource<Unit> _completionSource;
        public Task Task => _completionSource.Task;
        public void SetResult() => _completionSource.SetResult(Unit.Instance);
        public void TrySetResult() => _completionSource.TrySetResult(Unit.Instance);
        public void SetCanceled() => _completionSource.SetCanceled();
        public void TrySetCanceled() => _completionSource.TrySetCanceled();
        public void SetException(Exception exception) => _completionSource.SetException(exception);
        public void TrySetException(Exception exception) => _completionSource.TrySetException(exception);
        public void SetException(IEnumerable<Exception> exception) => _completionSource.SetException(exception);
        public void TrySetException(IEnumerable<Exception> exception) => _completionSource.TrySetException(exception);

        public AsyncTaskCompletionSource() => _completionSource = new AsyncTaskCompletionSource<Unit>();
        public AsyncTaskCompletionSource(TaskCreationOptions options) => _completionSource = new AsyncTaskCompletionSource<Unit>(options);
    }

    //Hack to implement the suggested framework fix from here: https://github.com/dotnet/runtime/issues/23405 so that calling cancel on a CancellationTokenSource does not call registrations synchronously.
}
