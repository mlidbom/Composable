using System;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    ///<summary>The monitor class exposes a rather horrifying API in my humble opinion. This class attempts to adapt it to something that is reasonably understandable and less brittle.</summary>
    public partial class MonitorCE
    {
        internal Lock EnterLock()
        {
            Enter();
            return _lock;
        }

        internal NotifyOneLock EnterNotifyOnlyOneUpdateLock()
        {
            Enter();
            return _notifyOneLock;
        }

        public NotifyAllLock EnterUpdateLock()
        {
            Enter();
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
