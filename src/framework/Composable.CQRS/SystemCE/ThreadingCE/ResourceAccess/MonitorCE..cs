﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

//An attribute is used rather than a pragma because the roslyn analyzers are confused by the multiple files of this partial class and keep calling
//the pragma redundant and still showing the warning about disposable fields. After moving the pragma to the file the analyzer wanted it in this
//time the warning goes away, only to come back with the next restart of visual studio
[assembly: SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable",
                           Justification = "By creating the locks only once in the constructor usages become zero-allocation operations. By always referencing them by the concrete type inlining remains possible.",
                           Scope = "type",
                           Target = "~T:Composable.SystemCE.ThreadingCE.ResourceAccess.MonitorCE")]

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    ///<summary>The monitor class exposes a rather obscure, brittle and easily misused API in my opinion. This class attempts to adapt it to something that is reasonably understandable and less brittle.</summary>
    public partial class MonitorCE
    {
        ulong _lockId;
        long _contendedLocks;
        readonly object _timeoutLock = new object();
        int _allowReentrancyIfGreaterThanZero;
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
            OnBeforeLockExit_Must_be_called_by_any_code_exiting_lock_including_waits();
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
            if(_allowReentrancyIfGreaterThanZero == 0 && IsEntered()) throw new InvalidOperationException($"{nameof(MonitorCE)} is not reentrant.");
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

            return true;
        }

        bool IsEntered() => Monitor.IsEntered(_lockObject);

        void OnBeforeLockExit_Must_be_called_by_any_code_exiting_lock_including_waits()
        {
            unchecked { _lockId++; }
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
