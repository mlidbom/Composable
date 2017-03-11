using System;
using System.Threading;
using Composable.Contracts;

namespace Composable.SystemExtensions.Threading
{
    ///<summary>Thrown if the <see cref="SingleThreadUseGuard"/> detects a thread change.</summary>
    class MultiThreadedUseException : InvalidOperationException
    {
        ///<summary>Constructs an instance using the supplied arguments to create an informative message.</summary>
        public MultiThreadedUseException(object guarded, Thread owningThread, Thread currentThread)
            : base(string.Format("Attempt to use {0} from thread Id:{1}, Name: {2} when owning thread was Id: {3} Name: {4}",
                                guarded, currentThread.ManagedThreadId, currentThread.Name, owningThread.ManagedThreadId, owningThread.Name))
        {
            ContractOptimized.Argument(guarded, nameof(guarded), owningThread, nameof(owningThread), currentThread, nameof(currentThread))
                             .NotNull();
        }
    }
}