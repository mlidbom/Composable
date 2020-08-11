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

        public void ScheduleContinuation(TResult result) => _completionSource.SetResult(result);
        public void ScheduleException(Exception exception) => _completionSource.SetException(exception);
    }

    class AsyncTaskCompletionSource
    {
        readonly AsyncTaskCompletionSource<VoidCE> _completionSource;
        public Task Task => _completionSource.Task;

        public void ScheduleContinuation() => _completionSource.ScheduleContinuation(VoidCE.Instance);
        public void ScheduleException(Exception exception) => _completionSource.ScheduleException(exception);

        public AsyncTaskCompletionSource() => _completionSource = new AsyncTaskCompletionSource<VoidCE>();
    }
}
