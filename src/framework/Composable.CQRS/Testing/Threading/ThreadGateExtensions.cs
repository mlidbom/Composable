using System;
using System.Threading.Tasks;
using System.Transactions;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Composable.Testing.Transactions;

namespace Composable.Testing.Threading
{
    static class ThreadGateExtensions
    {
        public static TResult AwaitPassthroughAndReturn<TResult>(this IThreadGate @this, TResult returnValue)
        {
            @this.AwaitPassThrough();
            return returnValue;
        }

        public static TResult AwaitPassthroughAndExecute<TResult>(this IThreadGate @this, Func<TResult> func)
        {
            @this.AwaitPassThrough();
            return func();
        }

        public static void AwaitPassthroughAndExecute(this IThreadGate @this, Action action)
        {
            @this.AwaitPassThrough();
            action();
        }

        public static IThreadGate Await(this IThreadGate @this, Func<bool> condition) => @this.Await(@this.DefaultTimeout, condition);
        public static IThreadGate Await(this IThreadGate @this, TimeSpan timeout, Func<bool> condition) => @this.ExecuteWithExclusiveLockWhen(timeout, condition, () => {});

        public static IThreadGate AwaitClosed(this IThreadGate @this) => @this.Await(() => !@this.IsOpen);
        public static bool TryAwaitClosed(this IThreadGate @this, TimeSpan timeout) => @this.TryAwait(timeout, () => !@this.IsOpen);

        public static IThreadGate AwaitQueueLengthEqualTo(this IThreadGate @this, int length) => @this.Await(() => @this.Queued == length);
        public static IThreadGate AwaitQueueLengthEqualTo(this IThreadGate @this, int length, TimeSpan timeout) => @this.Await(timeout, () => @this.Queued == length);
        public static bool TryAwaitQueueLengthEqualTo(this IThreadGate @this, int length, TimeSpan timeout) => @this.TryAwait(timeout, () => @this.Queued == length);

        public static IThreadGate AwaitPassedThroughCountEqualTo(this IThreadGate @this, int length) => @this.Await(() => @this.Passed == length);
        public static IThreadGate AwaitPassedThroughCountEqualTo(this IThreadGate @this, int length, TimeSpan timeout) => @this.Await(timeout, () => @this.Passed == length);
        public static bool TryAwaitPassededThroughCountEqualTo(this IThreadGate @this, int count, TimeSpan timeout) => @this.TryAwait(timeout, () => @this.Passed == count);

        public static IThreadGate AwaitEmptyQueue(this IThreadGate @this) => @this.Await(() => @this.Queued == 0);
        public static bool TryAwaitEmptyQueue(this IThreadGate @this, TimeSpan timeout) => @this.TryAwait(timeout, () => @this.Queued == 0);

        public static IThreadGate ThrowPostPassThrough(this IThreadGate @this, Exception exception) => @this.SetPostPassThroughAction(_ => throw exception);
        public static IThreadGate ThrowPrePassThrough(this IThreadGate @this, Exception exception) => @this.SetPostPassThroughAction(_ => throw exception);

        public static IThreadGate FailTransactionOnPreparePostPassThrough(this IThreadGate @this, Exception exception) => @this.SetPostPassThroughAction(_ => Transaction.Current.FailOnPrepare(exception));


        public static Task<IThreadGate> ThrowOnNextPassThroughAsync(this IThreadGate @this, Func<ThreadSnapshot, Exception> exceptionFactory)
        {
            var currentPassthroughAction = @this.PassThroughAction;
            var currentPassedThroughCountPlusOne = @this.PassedThrough.Count + 1;
            @this.SetPassThroughAction(threadSnapshot => throw exceptionFactory(threadSnapshot));
            return @this.ExecuteWithExclusiveLockWhenAsync(1.Minutes(), () => currentPassedThroughCountPlusOne == @this.PassedThrough.Count, () => @this.SetPassThroughAction(currentPassthroughAction));
        }

        public static Task<IThreadGate> ExecuteWithExclusiveLockWhenAsync(this IThreadGate @this, TimeSpan timeout, Func<bool> condition, Action action)
            => TaskCE.Run(() => @this.ExecuteWithExclusiveLockWhen(timeout, condition, action));

        public static IThreadGate WithExclusiveLock(this IThreadGate @this, Action action) => @this.ExecuteWithExclusiveLockWhen(@this.DefaultTimeout, () => true, action);
    }
}
