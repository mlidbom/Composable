using System;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    interface IResourceGuard
    {
        TimeSpan Timeout { get; }

        TResult Read<TResult>(Func<TResult> read);
        void Update(Action action);
        TResult Update<TResult>(Func<TResult> update);

        IResourceLock AwaitUpdateLockWhen(Func<bool> condition) => AwaitUpdateLockWhen(LockCE.InfiniteTimeout, condition);
        IResourceLock AwaitUpdateLockWhen(TimeSpan conditionTimeout, Func<bool> condition);

        IResourceLock AwaitUpdateLock() => AwaitUpdateLock(Timeout);
        IResourceLock AwaitUpdateLock(TimeSpan timeout);

        void Await(Func<bool> condition) => Await(LockCE.InfiniteTimeout, condition);
        bool TryAwait(Func<bool> condition) => TryAwait(LockCE.InfiniteTimeout, condition);

        void Await(TimeSpan conditionTimeout, Func<bool> condition);
        bool TryAwait(TimeSpan conditionTimeout, Func<bool> condition);

        void UpdateWhen(Func<bool> condition, Action action) => UpdateWhen(LockCE.InfiniteTimeout, condition, action);

        TResult UpdateWhen<TResult>(Func<bool> condition, Func<TResult> update) => UpdateWhen(LockCE.InfiniteTimeout, condition, update);

        void UpdateWhen(TimeSpan timeout, Func<bool> condition, Action action);
        TResult UpdateWhen<TResult>(TimeSpan timeout, Func<bool> condition, Func<TResult> update);
    }

    interface IResourceLock : IDisposable
    {
    }
}
