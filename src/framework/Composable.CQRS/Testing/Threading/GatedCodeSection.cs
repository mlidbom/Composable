using System;
using Composable.SystemCE;

namespace Composable.Testing.Threading
{
    class GatedCodeSection : IGatedCodeSection
    {
        public IThreadGate EntranceGate { get; }
        public IThreadGate ExitGate { get; }

        public static IGatedCodeSection WithTimeout(TimeSpan timeout) => new GatedCodeSection(timeout);

        GatedCodeSection(TimeSpan timeout)
        {
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
