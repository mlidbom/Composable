using System;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    interface IResourceGuard
    {
        TimeSpan Timeout { get; }
        TResult Read<TResult>(Func<TResult> read);
        void Update(Action action);
        TResult Update<TResult>(Func<TResult> update);

        IResourceLock AwaitUpdateLockWhen(Func<bool> condition) => AwaitUpdateLockWhen(Timeout, condition);
        IResourceLock AwaitUpdateLockWhen(TimeSpan timeout, Func<bool> condition);
        IResourceLock AwaitUpdateLock(TimeSpan? timeout = null);

        //Urgent: This makes no sense. The Timeout member is about lock timeouts, not about conditions.
        void Await(Func<bool> condition) => Await(Timeout, condition);
        bool TryAwait(Func<bool> condition) => TryAwait(Timeout, condition);

        void Await(TimeSpan timeOut, Func<bool> condition);
        bool TryAwait(TimeSpan timeOut, Func<bool> condition);

        void UpdateWhen(Func<bool> condition, Action action) => UpdateWhen(Timeout, condition, action);

        TResult UpdateWhen<TResult>(Func<bool> condition, Func<TResult> update) => UpdateWhen(Timeout, condition, update);

        void UpdateWhen(TimeSpan timeout, Func<bool> condition, Action action);
        TResult UpdateWhen<TResult>(TimeSpan timeout, Func<bool> condition, Func<TResult> update);
    }

    interface IResourceLock : IDisposable
    {
    }
}
