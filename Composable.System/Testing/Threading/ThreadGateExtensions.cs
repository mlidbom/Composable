using System;

namespace Composable.Testing.Threading
{
    static class ThreadGateExtensions
    {
        public static IThreadGate Await(this IThreadGate @this, Predicate<IThreadGate> condition) => @this.Await(@this.DefaultTimeout, condition);
        public static IThreadGate Await(this IThreadGate @this, TimeSpan timeout, Predicate<IThreadGate> condition) => @this.ExecuteLockedOnce(timeout, condition, (gate, owner) => {});
        public static IThreadGate AwaitClosed(this IThreadGate @this) => @this.Await(_ => !@this.IsOpen);
        public static IThreadGate AwaitQueueLength(this IThreadGate @this, int length) => @this.Await(me => me.Queued == length);
        public static IThreadGate AwaitEmptyQueue(this IThreadGate @this) => @this.Await(me => me.Queued == 0);
        public static IThreadGate WithExclusiveLock(this IThreadGate @this, Action action) => @this.ExecuteLockedOnce(@this.DefaultTimeout, _ => true, (gate, owner) => action());
    }
}
