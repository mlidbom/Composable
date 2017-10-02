using System;

namespace Composable.System.Threading.ResourceAccess
{
    static class ResourceAccessGuardExtensions
    {
        public static IExclusiveResourceLock AwaitExclusiveLockWhen(this IGuardedResource @this, TimeSpan timeout, Func<bool> condition)
        {
            IExclusiveResourceLock exclusiveLock = null;
            try
            {
                exclusiveLock = @this.AwaitExclusiveLock();
                while(!condition())
                {
                    exclusiveLock.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(timeout);
                }
                return exclusiveLock;
            }
            catch(AwaitingUpdateNotificationTimedOutException notificationTimeout)
            {
                exclusiveLock?.Dispose();
                throw new AwaitingConditionTimedOutException(notificationTimeout);
            }
            catch(Exception)
            {
                exclusiveLock?.Dispose();
                throw;
            }
        }

        public static void AwaitCondition(this IGuardedResource @this, TimeSpan timeout, Func<bool> condition)
        {
            using(@this.AwaitExclusiveLockWhen(timeout, condition)) {}
        }

        public static bool TryAwaitCondition(this IGuardedResource @this, TimeSpan timeout, Func<bool> condition)
        {
            var startTime = DateTime.Now;
            using(var @lock = @this.AwaitExclusiveLock(timeout))
            {
                while(!condition())
                {
                    if(DateTime.Now - startTime > timeout)
                    {
                        return false;
                    }
                    @lock.TryReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(timeout);
                }
                return true;
            }
        }


        public static TResult Read<TResult>(this IGuardedResource @this, Func<TResult> read)
        {
            using(@this.AwaitExclusiveLock())
            {
                return read();
            }
        }

        public static void Update(this IGuardedResource @this, Action action)
        {
            using(var @lock = @this.AwaitExclusiveLock())
            {
                action();
                @lock.NotifyWaitingThreadsAboutUpdate();
            }
        }

        public static TResult Update<TResult>(this IGuardedResource @this, Func<TResult> action)
        {
            using(var @lock = @this.AwaitExclusiveLock())
            {
                var result = action();
                @lock.NotifyWaitingThreadsAboutUpdate();
                return result;
            }
        }

        public static TResult UpdateAndReturn<TResult>(this IGuardedResource @this, Action action, TResult result)
            => @this.Update(() =>
            {
                action();
                return result;
            });

        public static TResult UpdateAndReturn<TResult>(this IGuardedResource @this, Action action, Func<TResult> resultFunction)
            => @this.Update(() =>
            {
                action();
                return resultFunction();
            });
    }

    class AwaitingConditionTimedOutException : Exception
    {
        public AwaitingConditionTimedOutException(AwaitingUpdateNotificationTimedOutException notificationTimeout) : base(
            "Timed out waiting for condiditon to become true. Never got any update notifications",
            innerException: notificationTimeout) {}
        public AwaitingConditionTimedOutException() : base("Timed out waiting for condiditon to become true.") {}
    }
}
