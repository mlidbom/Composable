using System;
using System.Threading;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    partial class MonitorCE
    {
        internal void EnterWhen(TimeSpan conditionTimeout, Func<bool> condition)
        {
            if(!TryEnterWhen(conditionTimeout, condition))
            {
                throw new AwaitingConditionTimedOutException();
            }
        }

        internal bool TryEnterWhen(TimeSpan conditionTimeout, Func<bool> condition)
        {
            var acquiredLockStartingWait = false;
            try
            {
                if(conditionTimeout == InfiniteTimeout)
                {
                    Enter(DefaultTimeout);
                    acquiredLockStartingWait = true;
                    Interlocked.Increment(ref _waitingThreadCount);
                    while(!condition()) Wait(InfiniteTimeout);
                } else
                {
                    var startTime = DateTime.UtcNow;
                    Enter(conditionTimeout);
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
    }
}
