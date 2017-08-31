using System;

namespace Composable.Testing.Threading
{
    static class ThreadGateExtensions
    {
        public static IThreadGate Await(this IThreadGate @this, Func<bool> condition) => @this.Await(@this.DefaultTimeout, condition);
        public static IThreadGate Await(this IThreadGate @this, TimeSpan timeout, Func<bool> condition) => @this.ExecuteLockedOnce(timeout, condition, (gate, owner) => {});
        public static IThreadGate AwaitClosed(this IThreadGate @this) => @this.Await(() => !@this.IsOpen);
        public static IThreadGate AwaitQueueLength(this IThreadGate @this, int length) => @this.Await(() => @this.Queued == length);
        public static IThreadGate AwaitEmptyQueue(this IThreadGate @this) => @this.Await(() => @this.Queued == 0);
        public static IThreadGate WithExclusiveLock(this IThreadGate @this, Action action) => @this.ExecuteLockedOnce(@this.DefaultTimeout, () => true, (gate, owner) => action());
    }
}
