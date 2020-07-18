using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.GenericAbstractions;

namespace Composable.System.Threading.Tasks
{
    class TaskCompletionSource
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

        public TaskCompletionSource(TaskCreationOptions options) => _completionSource  = new TaskCompletionSource<Unit>(options);
    }
}
