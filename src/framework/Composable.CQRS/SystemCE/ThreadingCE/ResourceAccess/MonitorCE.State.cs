using System;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    partial class MonitorCE
    {
        int _waitingThreadCount;
        readonly object _lockObject = new object();
        internal static readonly TimeSpan InfiniteTimeout = -1.Milliseconds();

#if NCRUNCH
        internal static readonly TimeSpan DefaultTimeout = 45.Seconds(); //Tests timeout at 60 seconds. We want locks to timeout faster so that the blocking stack traces turn up in the test output so we can diagnose the deadlocks.
#else
        internal static readonly TimeSpan DefaultTimeout = 2.Minutes(); //MsSql default query timeout is 30 seconds. Default .Net transaction timeout is 60. If we reach 2 minutes it is all but guaranteed that we have an in-memory deadlock.
#endif

        //Passing this to one of Monitor's Try* methods ensures that the method never blocks.
        internal static readonly TimeSpan NonBlockingTimeout = TimeSpan.Zero;

        internal readonly TimeSpan Timeout;

        MonitorCE(TimeSpan timeout)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            _lock = new Lock(this);
            _readLock = new ReadLock(this);
            _notifyOneUpdateLock = new NotifyOneUpdateLock(this);
            _notifyAllUpdateLock = new NotifyAllUpdateLock(this);
#pragma warning restore CS0618 // Type or member is obsolete
            Timeout = timeout;
        }
    }
}
