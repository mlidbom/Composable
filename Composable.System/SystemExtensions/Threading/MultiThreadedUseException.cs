using System;
using System.Diagnostics.Contracts;
using System.Threading;

namespace Composable.SystemExtensions.Threading
{
    public class MultiThreadedUseException : InvalidOperationException
    {
        public MultiThreadedUseException(object guarded, Thread owningThread, Thread currentThread)
            : base(string.Format("Atttempt to use {0} from thread Id:{1}, Name: {2} when owning thread was Id: {3} Name: {4}", 
                                guarded, currentThread.ManagedThreadId, currentThread.Name, owningThread.ManagedThreadId, owningThread.Name))
        {
            Contract.Requires(guarded != null && owningThread != null && currentThread != null);
        }
    }
}