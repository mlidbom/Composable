using System;
using System.Threading;

namespace Composable.SystemExtensions.Threading
{
    public class MultiThreadedUseException : InvalidOperationException
    {
        public MultiThreadedUseException(object guarded, Thread owningThread, Thread currentThread)
            : base(string.Format("Atttempt to use {0} from thread {1} when owning thread was {2}", guarded, currentThread, owningThread))
        {

        }
    }
}