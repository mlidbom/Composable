using System.Threading;

namespace Composable.SystemExtensions.Threading
{
    ///<summary>Ensures that guarded components are used within one thread only.</summary>
    class SingleThreadUseGuard : UsageGuard
    {
        readonly Thread _owningThread;

        ///<summary>Default constructor associates the instance with the current thread.</summary>
        public SingleThreadUseGuard()
        {
            _owningThread = Thread.CurrentThread;
        }

        ///<summary>Throws a <see cref="MultiThreadedUseException"/> if the current thread is different from the one that the instance was constructed in.</summary>
        protected override void InternalAssertNoChangeOccurred(object guarded)
        {
            if (Thread.CurrentThread != _owningThread)
            {
                throw new MultiThreadedUseException(guarded, _owningThread, Thread.CurrentThread);
            }
        }
    }
}