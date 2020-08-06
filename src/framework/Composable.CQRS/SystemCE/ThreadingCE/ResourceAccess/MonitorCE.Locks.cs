using System;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    ///<summary>The monitor class exposes a rather horrifying API in my humble opinion. This class attempts to adapt it to something that is reasonably understandable and less brittle.</summary>
#pragma warning disable CA1001 // Types that own disposable fields should be disposable, these fields hold no reference to anything but this instance. They are disposable only to enable non-allocating use of the using statement to manage locking.
    public partial class MonitorCE
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        internal Lock EnterReadLock()
        {
            EnterInternal();
            return _lock;
        }

        internal NotifyOneLock EnterNotifyOnlyOneUpdateLock()
        {
            EnterInternal();
            return _notifyOneLock;
        }

        public NotifyAllLock EnterUpdateLock()
        {
            EnterInternal();
            return _notifyAllLock;
        }

        ///<summary>Ensure you only call <see cref="Dispose"/> once on an instance.</summary>
        public sealed class NotifyAllLock : IDisposable
        {
            readonly MonitorCE _monitor;
            [Obsolete("Only for internal use")]
            internal NotifyAllLock(MonitorCE monitor) => _monitor = monitor;

            public void Dispose() => _monitor.NotifyAllExit();
        }

        ///<summary>Ensure you only call <see cref="Dispose"/> once on an instance.</summary>
        internal sealed class NotifyOneLock : IDisposable
        {
            readonly MonitorCE _monitor;
            [Obsolete("Only for internal use")]
            internal NotifyOneLock(MonitorCE monitor) => _monitor = monitor;

            public void Dispose() => _monitor.NotifyOneExit();
        }

        ///<summary>Ensure you only call <see cref="Dispose"/> once on an instance.</summary>
        internal sealed class Lock : IDisposable
        {
            readonly MonitorCE _monitor;
            [Obsolete("Only for internal use")]
            internal Lock(MonitorCE monitor) => _monitor = monitor;

            public void Dispose() => _monitor.Exit();
        }
    }
}
