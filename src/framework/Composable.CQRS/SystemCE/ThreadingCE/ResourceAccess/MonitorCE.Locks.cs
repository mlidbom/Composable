using System;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    ///<summary>The monitor class exposes a rather horrifying API in my humble opinion. This class attempts to adapt it to something that is reasonably understandable and less brittle.</summary>
#pragma warning disable CA1001 // Types that own disposable fields should be disposable, these fields hold no reference to anything but this instance. They are disposable only to enable non-allocating use of the using statement to manage locking.
    partial class MonitorCE
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        //By creating the locks only once in the constructor usages become zero-allocation operations. By always referencing them by the concrete type inlining remains possible.
        readonly Lock _lock;
        readonly ReadLock _readLock;
        readonly NotifyOneUpdateLock _notifyOneUpdateLock;
        readonly NotifyAllUpdateLock _notifyAllUpdateLock;

        internal Lock EnterLock()
        {
            Enter();
            return _lock;
        }

        internal NotifyOneUpdateLock EnterNotifyOneUpdateLock()
        {
            Enter();
            return _notifyOneUpdateLock;
        }

        internal NotifyAllUpdateLock EnterNotifyAllUpdateLock()
        {
            Enter();
            return _notifyAllUpdateLock;
        }

        internal ReadLock EnterReadLock()
        {
            Enter();
            return _readLock;
        }


        ///<summary>Ensure you only call <see cref="IDisposable.Dispose"/> once on an instance of a <see cref="ISingleUseDisposable"/>.</summary>
        internal interface ISingleUseDisposable : IDisposable {}

        internal sealed class ReadLock : ISingleUseDisposable
        {
            readonly MonitorCE _monitor;
            [Obsolete("Only for internal use")]
            internal ReadLock(MonitorCE monitor) => _monitor = monitor;

            public void Dispose()
            {
                _monitor.Exit();
            }
        }

        internal sealed class NotifyAllUpdateLock : ISingleUseDisposable
        {
            readonly MonitorCE _monitor;
            [Obsolete("Only for internal use")]
            internal NotifyAllUpdateLock(MonitorCE monitor) => _monitor = monitor;

            public void Dispose()
            {
                _monitor.NotifyWaitingExit(ResourceAccess.NotifyWaiting.All);
            }
        }

        internal sealed class NotifyOneUpdateLock : ISingleUseDisposable
        {
            readonly MonitorCE _monitor;
            [Obsolete("Only for internal use")]
            internal NotifyOneUpdateLock(MonitorCE monitor) => _monitor = monitor;

            public void Dispose()
            {
                _monitor.NotifyWaitingExit(ResourceAccess.NotifyWaiting.One);
            }
        }

        internal sealed class Lock : ISingleUseDisposable
        {
            readonly MonitorCE _monitor;
            [Obsolete("Only for internal use")]
            internal Lock(MonitorCE monitor) => _monitor = monitor;

            public void Dispose()
            {
                _monitor.Exit();
            }
        }
    }
}
