using System;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

namespace Composable.Testing.Threading
{
    class GatedCodeSection : IGatedCodeSection
    {
        readonly MonitorCE _lock;
        public IThreadGate EntranceGate { get; }
        public IThreadGate ExitGate { get; }

        public static IGatedCodeSection WithTimeout(TimeSpan timeout) => new GatedCodeSection(timeout);

        GatedCodeSection(TimeSpan timeout)
        {
            _lock = MonitorCE.WithTimeout(timeout);
            EntranceGate = ThreadGate.CreateClosedWithTimeout(timeout);
            ExitGate = ThreadGate.CreateClosedWithTimeout(timeout);
        }

        public IGatedCodeSection WithUpdateLock(Action action)
        {
            using(_lock.EnterNotifyAllLock())
            {
                //The reason for taking the lock is to inspect/modify both gates. So take the locks right away and ensure consistency throughout the action
                EntranceGate.WithExclusiveLock(() => ExitGate.WithExclusiveLock(action));
            }
            return this;
        }

        public IDisposable Enter()
        {
            EntranceGate.AwaitPassThrough();
            return DisposableCE.Create(() => ExitGate.AwaitPassThrough());
        }
    }
}
