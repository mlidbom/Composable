using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    //Urgent: The split between this and LockCE does not seem to really make sense.
    class ResourceGuard : IResourceGuard
    {
        public static IResourceGuard WithTimeout(TimeSpan timeout) => new ResourceGuard(MonitorCE.WithTimeout(timeout));
        public static IResourceGuard WithDefaultTimeout() => new ResourceGuard(MonitorCE.WithDefaultTimeout());
        public static IResourceGuard WithInfiniteTimeout() => new ResourceGuard(MonitorCE.WithInfiniteTimeout());

        readonly MonitorCE _monitor;
        ResourceGuard(MonitorCE monitor) => _monitor = monitor;

        public void Await(Func<bool> condition) =>
            _monitor.Await(MonitorCE.InfiniteTimeout, condition);

        public void Await(TimeSpan conditionTimeout, Func<bool> condition) =>
            _monitor.Await(conditionTimeout, condition);

        public bool TryAwait(TimeSpan conditionTimeout, Func<bool> condition) =>
            _monitor.TryAwait(conditionTimeout, condition);

        public TResult Read<TResult>(Func<TResult> read) =>
            _monitor.Read(read);

        public void Update(Action action) =>
            _monitor.Update(action);

        public TResult Update<TResult>(Func<TResult> update) =>
            _monitor.Update(update);

        public IResourceLock AwaitUpdateLockWhen(Func<bool> condition) =>
            _monitor.EnterNotifyAllLockWhen(MonitorCE.InfiniteTimeout, condition);

        public IResourceLock AwaitUpdateLockWhen(TimeSpan conditionTimeout, Func<bool> condition) =>
            _monitor.EnterNotifyAllLockWhen(conditionTimeout, condition);

        public IResourceLock AwaitUpdateLock() =>
            _monitor.EnterNotifyAllLock();
    }
}
