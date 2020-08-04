using System;
using System.Threading;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    enum SignalWaitingThreadsMode
    {
        None,
        One,
        All
    }

    ///<summary>The monitor class exposes a rather horrifying API in my humble opinion. This class attempts to adapt it to something that is reasonably understandable and less brittle.</summary>
    class LockCE
    {
        int _waitingThreadCount;
        readonly object _lockObject = new object();
        internal static readonly TimeSpan InfiniteTimeout = -1.Milliseconds();

#if NCRUNCH
        //Tests timeout at 60 seconds. We want locks to timeout faster so that the blocking stack traces turn up in the test output so we can diagnose the deadlocks.
        internal static readonly TimeSpan DefaultTimeout = 45.Seconds();

#else
        //MsSql default query timeout is 30 seconds. Default .Net transaction timeout is 60. If we reach 2 minutes it is all but guaranteed that we have an in-memory deadlock.
        internal static readonly TimeSpan DefaultTimeout = 2.Minutes();

#endif
        internal TimeSpan Timeout { get; set; }

        public static LockCE WithDefaultTimeout() => new LockCE(DefaultTimeout);
        public static LockCE WithInfiniteTimeout() => new LockCE(InfiniteTimeout);
        public static LockCE WithTimeout(TimeSpan defaultTimeout) => new LockCE(defaultTimeout);

        LockCE(TimeSpan defaultTimeout) => Timeout = defaultTimeout;

        internal void AwaitAndAcquire(TimeSpan timeout, Func<bool> condition)
        {
            if(!TryAwaitAndAcquire(timeout, condition))
            {
                throw new AwaitingConditionTimedOutException();
            }
        }

        internal bool TryAwaitAndAcquire(TimeSpan timeout, Func<bool> condition)
        {
            try
            {
                Interlocked.Increment(ref _waitingThreadCount);
                var startTime = DateTime.UtcNow;

                bool infiniteTimeout = timeout == InfiniteTimeout;
                //Urgent: We are using the timeout parameter for dramatically different things here. The time to wait for the initial lock before assuming deadlock and throwing, and the amount of time to wait for the condition to become true.
                Acquire(timeout);
                if(infiniteTimeout)
                {
                    while(!condition()) ReleaseWaitForSignalOrTimeoutAndReacquire(InfiniteTimeout);
                } else
                {
                    while(!condition())
                    {
                        var elapsedTime = DateTime.UtcNow - startTime;
                        var timeRemaining = timeout - elapsedTime;
                        if(elapsedTime > timeout)
                        {
                            Release();
                            return false;
                        }

                        ReleaseWaitForSignalOrTimeoutAndReacquire(timeRemaining);
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref _waitingThreadCount);
            }

            return true;
        }

        internal void Release() => Monitor.Exit(_lockObject);

        void ReleaseWaitForSignalOrTimeoutAndReacquire(TimeSpan timeout) { Monitor.Wait(_lockObject, timeout); }

        internal void SignalWaitingThreadsAndRelease(SignalWaitingThreadsMode signalWaitingThreadsMode)
        {
            SignalWaitingThreads(signalWaitingThreadsMode);
            Release();
        }

        internal void SignalWaitingThreads(SignalWaitingThreadsMode signalWaitingThreadsMode)
        {
            if(_waitingThreadCount == 0)
            {
                return; //Pulsing is relatively expensive. Let's avoid it when we can.
            }

            switch(signalWaitingThreadsMode)
            {
                case SignalWaitingThreadsMode.None:
                    break;
                case SignalWaitingThreadsMode.One:
                    Monitor.Pulse(_lockObject); //One thread blocked on Monitor.Wait for our _lockObject, if there is such a thread, will now try and reacquire the lock.
                    break;
                case SignalWaitingThreadsMode.All:
                    Monitor.PulseAll(_lockObject); //All threads blocked on Monitor.Wait for our _lockObject, if there are such threads, will now try and reacquire the lock.
                    break;
            }
        }

        internal void Acquire(TimeSpan? timeout)
        {
            if(!TryAcquire(timeout))
            {
                throw new AcquireLockTimeoutException();
            }
        }

        internal bool TryAcquire(TimeSpan? timeout)
        {
            var lockTaken = false;
            try
            {
                Monitor.TryEnter(_lockObject, timeout ?? Timeout, ref lockTaken);
            }
            catch(Exception) //It is rare, but apparently possible, for TryEnter to throw an exception after the lock is taken. So we need to catch it and call Monitor.Exit if that happens to avoid leaking locks.
            {
                if(lockTaken) Release();
                throw;
            }

            return lockTaken;
        }
    }
}
