using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    // urgent: Carefully review monitor documentation: https://docs.microsoft.com/en-us/dotnet/api/system.threading.monitor?view=netcore-3.1
    /*
     Todo:
Note: Blocking == Context switch == about 20 nanoseconds to acquire a lock just turned into something like a microsecond or two at the very least. That's 50 times slower AT LEAST.
   So: If we know that we are about to block, we can do other work here that would be unacceptable for the uncontended case.
     Such as 
        incrementing a counter of such instances of contention
        logging once per 10^x contended locks including the stack trace.

    TryEnter without a timeout value NEVER blocks and return a bool this lets us detect contention and act differently when we know we are about to block: https://tinyurl.com/y36v2fh6

Note: In these cases we are allowed to do relatively expensive work to diagnose what is severe application misbehavior, most likely a deadlock:
  1.We have timed out unconditionally (Not a Try* method) acquiring the lock..
  2. We are releasing a lock and see that others have timed out waiting for this lock.
    */

#pragma warning disable CA1001 // Class owns disposable fields but is not disposable
    ///<summary>The monitor class exposes a rather horrifying API in my humble opinion. This class attempts to adapt it to something that is reasonably understandable and less brittle.</summary>
    public partial class MonitorCE
    {
        ulong _lockId;
        long _contendedLocks;
        readonly object _timeoutLock = new object();
        IReadOnlyList<EnterLockTimeoutException> _timeOutExceptionsOnOtherThreads = new List<EnterLockTimeoutException>();

        void Enter() => Enter(_timeout);

        void Enter(TimeSpan timeout)
        {
            var currentLock = _lockId;
            if(!TryEnter(timeout))
            {
                RegisterAndThrowTimeoutException(currentLock);
            }
        }

        void Exit()
        {
            UpdateAnyRegisteredTimeoutExceptions();
            Monitor.Exit(_lockObject);
        }

        void NotifyOneExit()
        {
            NotifyOneWaitingThread();
            Exit();
        }

        void NotifyAllExit()
        {
            NotifyAllWaitingThreads();
            Exit();
        }

        bool TryEnter(TimeSpan timeout)
        {
            if(!Monitor.TryEnter(_lockObject)) //This will never block and calling it first improves performance quite a bit.
            {
                Interlocked.Increment(ref _contendedLocks);
                var lockTaken = false;
                try
                {
                    Monitor.TryEnter(_lockObject, timeout, ref lockTaken);
                }
                catch(Exception) //It is rare, but apparently possible, for TryEnter to throw an exception after the lock is taken. So we need to catch it and call Monitor.Exit if that happens to avoid leaking locks.
                {
                    if(lockTaken) Exit();
                    throw;
                }

                if(!lockTaken) return false;
            }

            unchecked { _lockId++; }

            return true;
        }

        void NotifyOneWaitingThread()
        {
            if(_waitingThreadCount > 0) Monitor.Pulse(_lockObject); //One thread blocked on Monitor.Wait for our _lockObject, will now try and reacquire the lock.
        }

        void NotifyAllWaitingThreads()
        {
            if(_waitingThreadCount > 1) Monitor.PulseAll(_lockObject);   //All threads blocked on Monitor.Wait for our _lockObject, if there are such threads, will now try and reacquire the lock.
            else if(_waitingThreadCount > 0) Monitor.Pulse(_lockObject); //One thread blocked on Monitor.Wait for our _lockObject, will now try and reacquire the lock.
        }

        void RegisterAndThrowTimeoutException(ulong currentLock)
        {
            lock(_timeoutLock)
            {
                var exception = new EnterLockTimeoutException(lockId: currentLock);
                ThreadSafe.AddToCopyAndReplace(ref _timeOutExceptionsOnOtherThreads, exception);
                throw exception;
            }
        }

        void UpdateAnyRegisteredTimeoutExceptions()
        {
            if(_timeOutExceptionsOnOtherThreads.Count > 0)
            {
                lock(_timeoutLock)
                {
                    var stackTrace = new StackTrace(fNeedFileInfo: true);
                    foreach(var exception in _timeOutExceptionsOnOtherThreads.Where(exception => exception.LockId == _lockId))
                    {
                        exception.SetBlockingThreadsDisposeStackTrace(stackTrace);
                    }

                    Interlocked.Exchange(ref _timeOutExceptionsOnOtherThreads, new List<EnterLockTimeoutException>());
                }
            }
        }
    }
}
