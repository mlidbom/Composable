using System;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    interface IResourceGuard
    {
        IExclusiveResourceLock AwaitExclusiveLock(TimeSpan? timeoutOverride = null);
        TimeSpan DefaultTimeout { get; }
        TResult WithExclusiveAccess<TResult>(Func<TResult> func);
        void WithExclusiveAccess(Action action);
        TResult Read<TResult>(Func<TResult> read);
        void Update(Action action);
        TResult Update<TResult>(Func<TResult> update);


        IExclusiveResourceLock AwaitExclusiveLockWhen(Func<bool> condition) => AwaitExclusiveLockWhen(DefaultTimeout, condition);
        IExclusiveResourceLock AwaitExclusiveLockWhen(TimeSpan timeout, Func<bool> condition);
        IResourceReadLock AwaitReadLockWhen(TimeSpan timeout, Func<bool> condition);
        IResourceReadLock AwaitReadLock(TimeSpan? timeout = null);

        IResourceUpdateLock AwaitUpdateLockWhen(Func<bool> condition) => AwaitUpdateLockWhen(DefaultTimeout, condition);
        IResourceUpdateLock AwaitUpdateLockWhen(TimeSpan timeout, Func<bool> condition);
        IResourceUpdateLock AwaitUpdateLock(TimeSpan? timeout = null);
    }

    interface IResourceLock : IDisposable {}

    interface IResourceUpdateLock : IDisposable
    {
    }

    interface IResourceReadLock : IDisposable
    {
    }

    interface IExclusiveResourceLock : IResourceLock
    {
        void NotifyAllWaitingThreads();
        void NotifyOneWaitingThread();

        //todo: These two timeouts are fundamentally different from the timeout waiting to get a lock.
        //Consider a better design. They should probably not share the default timeout designed for lock acquisition.
        void ReleaseAwaitNotificationAndReacquire(TimeSpan timeout);
        bool TryReleaseAwaitNotificationAndReacquire(TimeSpan timeout);
    }
}
