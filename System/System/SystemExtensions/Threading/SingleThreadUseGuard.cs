using System.Threading;

namespace Composable.SystemExtensions.Threading
{
    public class SingleThreadUseGuard : UsageGuard
    {
        private readonly Thread _owningThread;

        public SingleThreadUseGuard()
        {
            _owningThread = Thread.CurrentThread;
        }

        override protected void InternalAssertNoChangeOccurred(object guarded)
        {
            if (Thread.CurrentThread != _owningThread)
            {
                throw new MultiThreadedUseException(guarded, _owningThread, Thread.CurrentThread);
            }
        }
    }
}