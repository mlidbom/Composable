using System;

namespace Composable.System.Threading.ResourceAccess
{
    static class ResourceAccessGuardExtensions
    {
        public static IExclusiveResourceLock AwaitExclusiveLockWhen(this IExclusiveResourceAccessGuard @this, TimeSpan timeout, Func<bool> condition)
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

        public static void Await(this IExclusiveResourceAccessGuard @this, TimeSpan timeout, Func<bool> condition)
        {
            using(@this.AwaitExclusiveLockWhen(timeout, condition)) {}
        }

        public static bool TryAwait(this IExclusiveResourceAccessGuard @this, TimeSpan timeout, Func<bool> condition)
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

        public static void ExecuteWithResourceExclusivelyLockedAndNotifyWaitingThreadsAboutUpdate(this IExclusiveResourceAccessGuard @this, Action action)
        {
            using(var @lock = @this.AwaitExclusiveLock())
            {
                action();
                @lock.SendUpdateNotificationToAllThreadsAwaitingUpdateNotification();
            }
        }

        public static TResult ExecuteWithResourceExclusivelyLockedAndNotifyWaitingThreadsAboutUpdate<TResult>(this IExclusiveResourceAccessGuard @this, Func<TResult> action)
        {
            using(var @lock = @this.AwaitExclusiveLock())
            {
                var result = action();
                @lock.SendUpdateNotificationToAllThreadsAwaitingUpdateNotification();
                return result;
            }
        }

        public static void ExecuteWithResourceExclusivelyLocked(this IExclusiveResourceAccessGuard @lock, Action action)
        {
            using(@lock.AwaitExclusiveLock())
            {
                action();
            }
        }

        public static TResult ExecuteWithResourceExclusivelyLocked<TResult>(this IExclusiveResourceAccessGuard @lock, Func<TResult> function)
        {
            using(@lock.AwaitExclusiveLock())
            {
                return function();
            }
        }

        public static void ExecuteWithResourceExclusivelyLockedWhen(this IExclusiveResourceAccessGuard @this, Func<bool> condition, Action action)
        {
            using(var @lock = @this.AwaitExclusiveLock())
            {
                while(!condition())
                {
                    @lock.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock();
                }

                action();
            }
        }

        public static void ExecuteWithResourceExclusivelyLockedWhen(this IExclusiveResourceAccessGuard @this, TimeSpan timeout, Func<bool> condition, Action action)
        {
            using(@this.AwaitExclusiveLockWhen(timeout, condition))
            {
                action();
            }
        }

        public static TResult ExecuteWithResourceExclusivelyLockedWhen<TResult>(this IExclusiveResourceAccessGuard @this, Func<bool> condition, Func<TResult> function)
        {
            using(var @lock = @this.AwaitExclusiveLock())
            {
                while(!condition())
                {
                    @lock.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock();
                }

                return function();
            }
        }

        public static void ExecuteWithResourceExclusivelyLocked(this IExclusiveResourceAccessGuard @lock, TimeSpan timeout, Action action)
        {
            using(@lock.AwaitExclusiveLock())
            {
                action();
            }
        }

        public static TResult ExecuteWithResourceExclusivelyLocked<TResult>(this IExclusiveResourceAccessGuard @lock, TimeSpan timeout, Func<TResult> function)
        {
            using(@lock.AwaitExclusiveLock())
            {
                return function();
            }
        }
    }

    class AwaitingConditionTimedOutException : Exception
    {
        public AwaitingConditionTimedOutException(AwaitingUpdateNotificationTimedOutException notificationTimeout) : base(
            "Timed out waiting for condiditon to become true. Never got any update notifications",
            innerException: notificationTimeout) {}
        public AwaitingConditionTimedOutException() : base("Timed out waiting for condiditon to become true.") {}
    }
}
