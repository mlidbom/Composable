using System;
using System.Threading;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    partial class MonitorCE
    {
        internal Lock EnterLockWhen(TimeSpan conditionTimeout, Func<bool> condition)
        {
            EnterWhen(conditionTimeout, condition);
            return _lock;
        }

        internal NotifyOneLock EnterNotifyOneLockWhen(TimeSpan conditionTimeout, Func<bool> condition)
        {
            EnterWhen(conditionTimeout, condition);
            return _notifyOneLock;
        }

        internal NotifyAllLock EnterNotifyAllLockWhen(TimeSpan conditionTimeout, Func<bool> condition)
        {
            EnterWhen(conditionTimeout, condition);
            return _notifyAllLock;
        }

        public void Await(TimeSpan conditionTimeout, Func<bool> condition)
        {
            if(!TryEnterWhen(conditionTimeout, condition))
            {
                throw new AwaitingConditionTimedOutException();
            }
            Exit();
        }

        public bool TryAwait(TimeSpan conditionTimeout, Func<bool> condition)
        {
            if(TryEnterWhen(conditionTimeout, condition))
            {
                Exit();
                return true;
            } else
            {
                return false;
            }
        }

        void EnterWhen(TimeSpan conditionTimeout, Func<bool> condition)
        {
            if(!TryEnterWhen(conditionTimeout, condition))
            {
                throw new AwaitingConditionTimedOutException();
            }
        }

        bool TryEnterWhen(TimeSpan conditionTimeout, Func<bool> condition)
        {
            var acquiredLockStartingWait = false;
            try
            {
                if(conditionTimeout == InfiniteTimeout)
                {
                    EnterInternal(DefaultTimeout);
                    acquiredLockStartingWait = true;
                    Interlocked.Increment(ref _waitingThreadCount);
                    while(!condition()) Wait(InfiniteTimeout);
                } else
                {
                    var startTime = DateTime.UtcNow;
                    EnterInternal(conditionTimeout);
                    acquiredLockStartingWait = true;
                    Interlocked.Increment(ref _waitingThreadCount);
                    while(!condition())
                    {
                        var elapsedTime = DateTime.UtcNow - startTime;
                        var timeRemaining = conditionTimeout - elapsedTime;
                        if(elapsedTime > conditionTimeout)
                        {
                            Exit();
                            return false;
                        }

                        Wait(timeRemaining);
                    }
                }
            }
            finally
            {
                if(acquiredLockStartingWait) Interlocked.Decrement(ref _waitingThreadCount);
            }

            return true;
        }

        void Wait(TimeSpan timeout) => Monitor.Wait(_lockObject, timeout);
    }
}
