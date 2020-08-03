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

        TimeSpan DefaultTimeout { get; set; }

        public static LockCE Create() => new LockCE(TimeSpan.MaxValue);
        public static LockCE WithTimeout(TimeSpan defaultTimeout) => new LockCE(defaultTimeout);

        LockCE(TimeSpan defaultTimeout) => DefaultTimeout = defaultTimeout;

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

                AcquireLock(timeout);
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
            finally
            {
                Interlocked.Decrement(ref _waitingThreadCount);
            }

            return true;
        }

        internal void Release() => Monitor.Exit(_lockObject);

        void ReleaseWaitForSignalOrTimeoutAndReacquire(TimeSpan timeRemaining) => Monitor.Wait(_lockObject, timeRemaining);

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

        internal void AcquireLock(TimeSpan? timeout)
        {
            if(!TryAcquireLock(timeout))
            {
                throw new AcquireLockTimeoutException();
            }
        }

        internal bool TryAcquireLock(TimeSpan? timeout)
        {
            var lockTaken = false;
            try
            {
                Monitor.TryEnter(_lockObject, timeout ?? DefaultTimeout, ref lockTaken);
            }
            catch(Exception) //It is rare, but apparently possible, for TryEnter to throw an exception after the lock is taken. So we do need to catch it and call Monitor.Exit if that happens to avoid leaking locks.
            {
                if(lockTaken) Release();
                throw;
            }

            return lockTaken;
        }
    }
}
