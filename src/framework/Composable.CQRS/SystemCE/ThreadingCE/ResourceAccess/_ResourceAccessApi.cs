using System;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    interface IResourceGuard
    {
        TimeSpan DefaultTimeout { get; }
        TResult Read<TResult>(Func<TResult> read);
        void Update(Action action);
        TResult Update<TResult>(Func<TResult> update);

        IResourceLock AwaitReadLockWhen(TimeSpan timeout, Func<bool> condition);
        IResourceLock AwaitReadLock(TimeSpan? timeout = null);

        IResourceLock AwaitUpdateLockWhen(Func<bool> condition) => AwaitUpdateLockWhen(DefaultTimeout, condition);
        IResourceLock AwaitUpdateLockWhen(TimeSpan timeout, Func<bool> condition);
        IResourceLock AwaitUpdateLock(TimeSpan? timeout = null);
    }

    interface IResourceLock : IDisposable
    {
    }
}
