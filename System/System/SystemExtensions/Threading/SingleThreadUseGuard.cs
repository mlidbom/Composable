using System.Threading;

namespace Composable.SystemExtensions.Threading
{
    public class SingleThreadUseGuard : ISingleContextUseGuard
    {
        private readonly Thread _owningThread;

        public SingleThreadUseGuard()
        {
            _owningThread = Thread.CurrentThread;
        }

        public void AssertNoContextChangeOccurred(object guarded)
        {
            if (Thread.CurrentThread != _owningThread)
            {
                throw new MultiThreadedUseException(guarded, _owningThread, Thread.CurrentThread);
            }
        }
    }
}