using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    enum NotifyWaiting
    {
        None,
        One,
        All
    }

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
        public static MonitorCE WithDefaultTimeout() => new MonitorCE(DefaultTimeout);
        public static MonitorCE WithInfiniteTimeout() => new MonitorCE(InfiniteTimeout);
        public static MonitorCE WithTimeout(TimeSpan defaultTimeout) => new MonitorCE(defaultTimeout);

        internal void Exit() => Monitor.Exit(_lockObject);

        void Wait(TimeSpan timeout) => Monitor.Wait(_lockObject, timeout);

        ///<summary>Note that while Signal calls <see cref="Monitor.PulseAll"/> it only does so if there are waiting threads. There is no overhead if there are no waiting threads.</summary>
        internal void NotifyWaitingExit(NotifyWaiting notifyWaiting)
        {
            NotifyWaiting(notifyWaiting);
            Exit();
        }

        internal void NotifyWaiting(NotifyWaiting notifyWaiting)
        {
            if(_waitingThreadCount == 0)
            {
                return; //Pulsing is relatively expensive. About 100 nanoseconds. 5 times the cost of acquiring an uncontended lock. We should definitely avoid it when we can.
            }

            switch(notifyWaiting)
            {
                case ResourceAccess.NotifyWaiting.None:
                    break;
                case ResourceAccess.NotifyWaiting.One:
                    Monitor.Pulse(_lockObject); //One thread blocked on Monitor.Wait for our _lockObject, if there is such a thread, will now try and reacquire the lock.
                    break;
                case ResourceAccess.NotifyWaiting.All:
                    Monitor.PulseAll(_lockObject); //All threads blocked on Monitor.Wait for our _lockObject, if there are such threads, will now try and reacquire the lock.
                    break;
            }
        }

        internal void Update(Action action)
        {
            Enter();
            try
            {
                action();
            }
            finally
            {
                NotifyWaiting(ResourceAccess.NotifyWaiting.All);
                Exit();
            }
        }

        internal T Update<T>(Func<T> func)
        {
            Enter();
            try
            {
                return func();
            }
            finally
            {
                NotifyWaiting(ResourceAccess.NotifyWaiting.All);
                Exit();
            }
        }

        internal TReturn Read<TReturn>(Func<TReturn> func)
        {
            Enter();
            try
            {
                return func();
            }
            finally
            {
                Exit();
            }
        }

        internal void Enter() => Enter(Timeout);

        internal void Enter(TimeSpan timeout)
        {
            if(!TryEnter(timeout))
            {
                throw new AcquireLockTimeoutException();
            }
        }

        ///<summary>Tries to enter the monitor. Will never block.</summary>
        internal bool TryEnterNonBlocking() => Monitor.TryEnter(_lockObject);

        internal bool TryEnter(TimeSpan timeout)
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
    }
}
