using System;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    interface IResourceGuard
    {
        TResult Read<TResult>(Func<TResult> read);
        void Update(Action action);
        TResult Update<TResult>(Func<TResult> update);

        IResourceLock AwaitUpdateLockWhen(Func<bool> condition);
        IResourceLock AwaitUpdateLockWhen(TimeSpan conditionTimeout, Func<bool> condition);

        IResourceLock AwaitUpdateLock();

        void Await(Func<bool> condition);
        void Await(TimeSpan conditionTimeout, Func<bool> condition);
        bool TryAwait(TimeSpan conditionTimeout, Func<bool> condition);
    }

    interface IResourceLock : IDisposable
    {
    }
}
