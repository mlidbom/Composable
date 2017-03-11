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
            : base(
                   $"Attempt to use {guarded} from thread Id:{currentThread.ManagedThreadId}, Name: {currentThread.Name} when owning thread was Id: {owningThread.ManagedThreadId} Name: {owningThread.Name}")
        {
            ContractOptimized.Argument(guarded, nameof(guarded), owningThread, nameof(owningThread), currentThread, nameof(currentThread))
                             .NotNull();
        }
    }
}