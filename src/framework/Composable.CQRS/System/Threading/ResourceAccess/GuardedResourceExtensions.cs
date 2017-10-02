using System;

namespace Composable.System.Threading.ResourceAccess
{
    static class GuardedResourceExtensions
    {
        static IExclusiveResourceLock AwaitExclusiveLockWhen(this IGuardedResource @this, TimeSpan timeout, Func<bool> condition)
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

        public static IResourceUpdateLock AwaitUpdateLock(this IGuardedResource @this, TimeSpan? timeout = null)
            => new UpdateResourceLock(@this.AwaitExclusiveLock(timeout));

        public static IResourceReadLock AwaitReadLock(this IGuardedResource @this, TimeSpan? timeout = null)
            => new ReadResourceLock(@this.AwaitExclusiveLock(timeout));

        public static IResourceUpdateLock AwaitUpdateLockWhen(this IGuardedResource @this, Func<bool> condition)
            => new UpdateResourceLock(@this.AwaitExclusiveLockWhen(@this.DefaultTimeout, condition));

        public static IResourceUpdateLock AwaitUpdateLockWhen(this IGuardedResource @this, TimeSpan timeout, Func<bool> condition)
            => new UpdateResourceLock(@this.AwaitExclusiveLockWhen(timeout, condition));

        public static IResourceReadLock AwaitReadLockWhen(this IGuardedResource @this, TimeSpan timeout, Func<bool> condition)
            => new ReadResourceLock(@this.AwaitExclusiveLockWhen(timeout, condition));

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
            using(@this.AwaitReadLock())
            {
                return read();
            }
        }

        public static void Update(this IGuardedResource @this, Action action)
        {
            using(@this.AwaitUpdateLock())
            {
                action();
            }
        }

        public static void UpdateWhen(this IGuardedResource @this, Func<bool> condition, Action action)
        {
            using (@this.AwaitUpdateLockWhen(@this.DefaultTimeout, condition))
            {
                action();
            }
        }

        public static TResult ReadWhen<TResult>(this IGuardedResource @this, TimeSpan timeout, Func<TResult> read, Func<bool> condition)
        {
            using (@this.AwaitUpdateLockWhen(timeout, condition))
            {
                return read();
            }
        }

        public static TResult Update<TResult>(this IGuardedResource @this, Func<TResult> action)
        {
            using(@this.AwaitUpdateLock())
            {
                return action();
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

        class ReadResourceLock : IResourceReadLock
        {
            readonly IExclusiveResourceLock _lock;
            public ReadResourceLock(IExclusiveResourceLock @lock) => _lock = @lock;

            public void Dispose() { _lock.Dispose(); }
        }

        class UpdateResourceLock : IResourceUpdateLock
        {
            readonly IExclusiveResourceLock _lock;
            public UpdateResourceLock(IExclusiveResourceLock @lock) => _lock = @lock;

            public void Dispose()
            {
                _lock.NotifyWaitingThreadsAboutUpdate();
                _lock.Dispose();
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
