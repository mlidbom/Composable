using System;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    interface IResourceGuard
    {
        TimeSpan DefaultTimeout { get; }
        TResult Read<TResult>(Func<TResult> read);
        void Update(Action action);
        TResult Update<TResult>(Func<TResult> update);

        IResourceLock AwaitUpdateLockWhen(Func<bool> condition) => AwaitUpdateLockWhen(DefaultTimeout, condition);
        IResourceLock AwaitUpdateLockWhen(TimeSpan timeout, Func<bool> condition);
        IResourceLock AwaitUpdateLock(TimeSpan? timeout = null);

        void Await(Func<bool> condition) => Await(DefaultTimeout, condition);
        bool TryAwait(Func<bool> condition) => TryAwait(DefaultTimeout, condition);

        void Await(TimeSpan timeOut, Func<bool> condition);
        bool TryAwait(TimeSpan timeOut, Func<bool> condition);

        void UpdateWhen(Func<bool> condition, Action action) => UpdateWhen(DefaultTimeout, condition, action);

        TResult UpdateWhen<TResult>(Func<bool> condition, Func<TResult> update) => UpdateWhen(DefaultTimeout, condition, update);

        void UpdateWhen(TimeSpan timeout, Func<bool> condition, Action action);
        TResult UpdateWhen<TResult>(TimeSpan timeout, Func<bool> condition, Func<TResult> update);
    }

    interface IResourceLock : IDisposable
    {
    }
}
