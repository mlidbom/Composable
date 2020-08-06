﻿using System;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    ///<summary>The monitor class exposes a rather horrifying API in my humble opinion. This class attempts to adapt it to something that is reasonably understandable and less brittle.</summary>
#pragma warning disable CA1001 // Types that own disposable fields should be disposable, these fields hold no reference to anything but this instance. They are disposable only to enable non-allocating use of the using statement to manage locking.
    partial class MonitorCE
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        internal Lock EnterLock()
        {
            EnterInternal();
            return _lock;
        }

        internal NotifyOneLock EnterNotifyOneLock()
        {
            EnterInternal();
            return _notifyOneLock;
        }

        internal NotifyAllLock EnterNotifyAllLock()
        {
            EnterInternal();
            return _notifyAllLock;
        }


        ///<summary>Ensure you only call <see cref="IDisposable.Dispose"/> once on an instance of a <see cref="ISingleUseDisposable"/>.</summary>
        internal interface ISingleUseDisposable : IDisposable {}


        //Urgent: Get rid of the IResourceLock interface
        internal sealed class NotifyAllLock : ISingleUseDisposable, IResourceLock
        {
            readonly MonitorCE _monitor;
            [Obsolete("Only for internal use")]
            internal NotifyAllLock(MonitorCE monitor) => _monitor = monitor;

            public void Dispose() => _monitor.NotifyWaitingExit(NotifyWaiting.All);
        }

        internal sealed class NotifyOneLock : ISingleUseDisposable, IResourceLock
        {
            readonly MonitorCE _monitor;
            [Obsolete("Only for internal use")]
            internal NotifyOneLock(MonitorCE monitor) => _monitor = monitor;

            public void Dispose() => _monitor.NotifyWaitingExit(NotifyWaiting.One);
        }

        internal sealed class Lock : ISingleUseDisposable, IResourceLock
        {
            readonly MonitorCE _monitor;
            [Obsolete("Only for internal use")]
            internal Lock(MonitorCE monitor) => _monitor = monitor;

            public void NotifyOneWaitingThread() => _monitor.NotifyOneWaitingThread();

            public void NotifyAllWaitingThread() => _monitor.NotifyAllWaitingThreads();

            public void Dispose() => _monitor.Exit();
        }
    }
}
