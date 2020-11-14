using System;
using System.Threading;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    public partial class MonitorCE
    {
        internal NotifyAllLock EnterUpdateLockWhen(Func<bool> condition) =>
            EnterUpdateLockWhen(InfiniteTimeout, condition);

        internal NotifyAllLock EnterUpdateLockWhen(TimeSpan conditionTimeout, Func<bool> condition)
        {
            EnterWhen(conditionTimeout, condition);
            return _notifyAllLock;
        }

        internal void Await(Func<bool> condition) => Await(InfiniteTimeout, condition);

        internal void Await(TimeSpan conditionTimeout, Func<bool> condition)
        {
            if(!TryAwait(conditionTimeout, condition))
            {
                throw new AwaitingConditionTimeoutException();
            }
        }

        internal bool TryAwait(TimeSpan conditionTimeout, Func<bool> condition)
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
                throw new AwaitingConditionTimeoutException();
            }
        }

        bool TryEnterWhen(TimeSpan conditionTimeout, Func<bool> condition)
        {
            var acquiredLockStartingWait = false;

            bool CheckConditionWhileAllowingReentrancy()
            {
                try
                {
                    _allowReentrancyIfGreaterThanZero++;
                    return condition();
                }
                finally
                {
                    _allowReentrancyIfGreaterThanZero--;
                }
            }

            try
            {
                if(conditionTimeout == InfiniteTimeout)
                {
                    Enter(DefaultTimeout);
                    acquiredLockStartingWait = true;
                    Interlocked.Increment(ref _waitingThreadCount);
                    while(!CheckConditionWhileAllowingReentrancy()) Wait(InfiniteTimeout);
                } else
                {
                    var startTime = DateTime.UtcNow;
                    Enter(conditionTimeout);
                    acquiredLockStartingWait = true;
                    Interlocked.Increment(ref _waitingThreadCount);

                    while(!CheckConditionWhileAllowingReentrancy())
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

        void Wait(TimeSpan timeout)
        {
            OnBeforeLockExit_Must_be_called_by_any_code_exiting_lock_including_waits();
            Monitor.Wait(_lockObject, timeout);
        }
    }
}
