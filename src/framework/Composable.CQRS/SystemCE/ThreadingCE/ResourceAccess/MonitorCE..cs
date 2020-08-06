using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    // urgent: Carefully review monitor documentation: https://docs.microsoft.com/en-us/dotnet/api/system.threading.monitor?view=netcore-3.1
    /*
Note: Blocking == Context switch == about 20 nanoseconds to acquire a lock just turned into something like a microsecond or two at the very least. That's 50 times slower AT LEAST.
   So: If we know that we are about to block, we can do other work here that would be unacceptable for the uncontended case.
     Such as 
        incrementing a counter of such instances of contention
        logging once per 10^x contended locks including the stack trace.

    TryEnter without a timeout value NEVER blocks and return a bool this lets us detect contention and act differently when we know we are about to block: https://tinyurl.com/y36v2fh6

Note: In these cases we are allowed to do expensive work, as is SECONDS if required instead of nanoseconds, to diagnose what is severe application misbehavior, most likely a deadlock:
  1.We have timed out unconditionally (Not a Try* method) acquiring the lock..
  2. We are releasing a lock and see that others have timed out waiting for this lock.
    */

    ///<summary>The monitor class exposes a rather horrifying API in my humble opinion. This class attempts to adapt it to something that is reasonably understandable and less brittle.</summary>
    partial class MonitorCE
    {
        readonly List<EnterLockTimeoutException> _timeOutExceptionsOnOtherThreads = new List<EnterLockTimeoutException>();
        int _timeoutsThrownDuringCurrentLock;

        void EnterInternal() => EnterInternal(Timeout);

        void EnterInternal(TimeSpan timeout)
        {
            if(!TryEnter(timeout))
            {
                RegisterAndThrowTimeoutException();
            }
        }

        void Exit()
        {
            SetBlockingStackTraceInTimedOutExceptions();
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

        ///<summary>Tries to enter the monitor. Will never block.</summary>
        bool TryEnterNonBlocking() => Monitor.TryEnter(_lockObject);

        bool TryEnter(TimeSpan timeout)
        {
            if(TryEnterNonBlocking()) return true;

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

            return lockTaken;
        }

        void NotifyOneWaitingThread()
        {
            if(_waitingThreadCount > 0)Monitor.Pulse(_lockObject); //One thread blocked on Monitor.Wait for our _lockObject, will now try and reacquire the lock.
        }

        void NotifyAllWaitingThreads()
        {
            if(_waitingThreadCount > 1)Monitor.PulseAll(_lockObject); //All threads blocked on Monitor.Wait for our _lockObject, if there are such threads, will now try and reacquire the lock.
            else if(_waitingThreadCount > 0) Monitor.Pulse(_lockObject); //One thread blocked on Monitor.Wait for our _lockObject, will now try and reacquire the lock.
        }

        void RegisterAndThrowTimeoutException()
        {
            lock(_timeOutExceptionsOnOtherThreads)
            {
                Interlocked.Increment(ref _timeoutsThrownDuringCurrentLock);
                var exception = new EnterLockTimeoutException();
                _timeOutExceptionsOnOtherThreads.Add(exception);
                throw exception;
            }
        }

        void SetBlockingStackTraceInTimedOutExceptions()
        {
            //todo: Log a warning if disposing after a longer time than the default lock timeout.
            //Using this exchange trick spares us from taking one more lock every time we release one at the cost of a negligible decrease in the chances for an exception to contain the blocking stacktrace.
            var timeoutExceptionsOnOtherThreads = Interlocked.Exchange(ref _timeoutsThrownDuringCurrentLock, 0);

            if(timeoutExceptionsOnOtherThreads > 0)
            {
                lock(_timeOutExceptionsOnOtherThreads)
                {
                    var stackTrace = new StackTrace(fNeedFileInfo: true);
                    foreach(var exception in _timeOutExceptionsOnOtherThreads)
                    {
                        exception.SetBlockingThreadsDisposeStackTrace(stackTrace);
                    }

                    _timeOutExceptionsOnOtherThreads.Clear();
                }
            }
        }
    }
}
