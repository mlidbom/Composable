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

        public IDisposable Enter()
        {
            EntranceGate.AwaitPassThrough();
            return DisposableCE.Create(() => ExitGate.AwaitPassThrough());
        }
    }
}
