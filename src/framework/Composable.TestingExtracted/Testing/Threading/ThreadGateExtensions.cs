using System;

namespace Composable.Testing.Testing.Threading
{
    public static class ThreadGateExtensions
    {
        public static IThreadGate Await(this IThreadGate @this, Func<bool> condition) => @this.Await(@this.DefaultTimeout, condition);
        public static IThreadGate Await(this IThreadGate @this, TimeSpan timeout, Func<bool> condition) => @this.ExecuteLockedOnce(timeout, condition, (gate, owner) => {});

        public static IThreadGate AwaitClosed(this IThreadGate @this) => @this.Await(() => !@this.IsOpen);
        public static bool TryAwaitClosed(this IThreadGate @this, TimeSpan timeout) => @this.TryAwait(timeout, () => !@this.IsOpen);

        public static IThreadGate AwaitQueueLength(this IThreadGate @this, int length) => @this.Await(() => @this.Queued >= length);
        public static bool TryAwaitQueueLengthExceeding(this IThreadGate @this, int length, TimeSpan timeout) => @this.TryAwait(timeout, () => @this.Queued >= length);

        public static IThreadGate AwaitPassedCount(this IThreadGate @this, int length) => @this.Await(() => @this.Passed >= length);
        public static IThreadGate AwaitPassedCount(this IThreadGate @this, int length, TimeSpan timeout) => @this.Await(timeout, () => @this.Passed >= length);
        public static bool TryPassedCount(this IThreadGate @this, int count, TimeSpan timeout) => @this.TryAwait(timeout, () => @this.Passed >= count);

        public static IThreadGate AwaitEmptyQueue(this IThreadGate @this) => @this.Await(() => @this.Queued == 0);
        public static bool TryAwaitEmptyQueue(this IThreadGate @this, TimeSpan timeout) => @this.TryAwait(timeout, () => @this.Queued == 0);



        public static IThreadGate WithExclusiveLock(this IThreadGate @this, Action action) => @this.ExecuteLockedOnce(@this.DefaultTimeout, () => true, (gate, owner) => action());
    }
}
