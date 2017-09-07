using System;
using Composable.Testing.System;
using Composable.Testing.System.Threading.ResourceAccess;

namespace Composable.Testing.Testing.Threading
{
    class GatedCodeSection : IGatedCodeSection
    {
        readonly IExclusiveResourceAccessGuard _lock;
        public IThreadGate EntranceGate { get; }
        public IThreadGate ExitGate { get; }

        public static IGatedCodeSection WithTimeout(TimeSpan timeout) => new GatedCodeSection(timeout);

        GatedCodeSection(TimeSpan timeout)
        {
            _lock = ResourceAccessGuard.ExclusiveWithTimeout(timeout);
            EntranceGate = ThreadGate.WithTimeout(timeout);
            ExitGate = ThreadGate.WithTimeout(timeout);
        }

        public IGatedCodeSection WithExclusiveLock(Action action)
        {
            using(_lock.AwaitExclusiveLock())
            {
                //The reason for taking the lock is to inspect/modify both gates. So take the locks right away and ensure consistency throughout the action
                EntranceGate.WithExclusiveLock(() => ExitGate.WithExclusiveLock(action));
            }
            return this;
        }

        public IDisposable Enter()
        {
            EntranceGate.Pass();
            return Disposable.Create(() => ExitGate.Pass());
        }
    }
}
