using System.Threading;

namespace Composable.SystemExtensions.Threading
{
    public class SingleThreadedUseGuard
    {
        private readonly object _guarded;
        private readonly Thread _owningThread;

        public SingleThreadedUseGuard(object guarded)
        {
            _guarded = guarded;
            _owningThread = Thread.CurrentThread;
        }

        public void AssertNoThreadChangeOccurred()
        {
            if (Thread.CurrentThread != _owningThread)
            {
                throw new MultiThreadedUseException(_guarded, _owningThread, Thread.CurrentThread);
            }
        }
    }
}