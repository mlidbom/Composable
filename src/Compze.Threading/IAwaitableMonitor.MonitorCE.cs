using System.Diagnostics;
using Compze.Threading.Exceptions;
using Compze.Threading._internal.Utilities;
using Compze.Threading.Exceptions._private;

namespace Compze.Threading;

public partial interface IAwaitableMonitor
{
#pragma warning disable CA1001 //By creating the locks only once in the constructor usages become zero-allocation operations.
#pragma warning disable CS0618 // Type or member is obsolete
   private class MonitorCE : IAwaitableMonitor, IMonitor, ILockInternals
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CA1001
   {
      public ILock TakeLock(CancellationToken cancellationToken = default, LockTimeout? timeout = null) => TakeLock(LockType.Read, timeout, cancellationToken);
      public IReadLock TakeReadLock(CancellationToken cancellationToken = default, LockTimeout? timeout = null) => TakeLock(LockType.Read, timeout, cancellationToken);
      public IUpdateLock TakeUpdateLock(CancellationToken cancellationToken = default, LockTimeout? timeout = null) => TakeLock(LockType.Update, timeout, cancellationToken);

      public IReadLock TakeReadLockWhen(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TakeLockWhen(condition, LockType.Read, cancellationToken, waitTimeout, lockTimeout);

      public IUpdateLock TakeUpdateLockWhen(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TakeLockWhen(condition, LockType.Update, cancellationToken, waitTimeout, lockTimeout);

      public IReadLock? TryTakeReadLockWhen(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TryTakeLockWhen(condition, LockType.Read, cancellationToken, waitTimeout, lockTimeout);

      public IUpdateLock? TryTakeUpdateLockWhen(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TryTakeLockWhen(condition, LockType.Update, cancellationToken, waitTimeout, lockTimeout);

      public LockTimeout LockTimeout { get; }
      public WaitTimeout WaitTimeout { get; }
      public long ContentionCount => _monitor.ContentionCount;

      public void SetTimeToWaitForStackTrace(WaitTimeout timeToWaitForStackTrace) => _stackTraceFetchTimeout = timeToWaitForStackTrace;

      readonly ThinMonitorWrapper _monitor = new();
      readonly Lock _timeoutLock = new();

      readonly LockDisposer _readLock;
      readonly LockDisposer _updateLock;

      static readonly WaitTimeout DefaultTimeToWaitForStackTrace = WaitTimeout.Seconds(10);

      WaitTimeout _stackTraceFetchTimeout;

      public MonitorCE(LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null)
      {
         LockTimeout = lockTimeout ?? LockTimeout.Default;
         WaitTimeout = waitTimeout ?? WaitTimeout.Default;
         _readLock = new LockDisposer(ReleaseLock);
         _updateLock = new LockDisposer(() =>
         {
            _monitor.NotifyWaitingThreadsAboutUpdates(); //All threads blocked on Monitor.Wait for our _lockObject will now try and reacquire the lock
            ReleaseLock();
         });
         _stackTraceFetchTimeout = DefaultTimeToWaitForStackTrace;
      }

      LockDisposer LockFor(LockType lockType)
      {
         return lockType switch
         {
            LockType.Read   => _readLock,
            LockType.Update => _updateLock,
            _               => throw new ArgumentOutOfRangeException(nameof(lockType), lockType, message: null)
         };
      }

      LockDisposer TakeLock(LockType lockType, LockTimeout? lockTimeout = null, CancellationToken cancellationToken = default) =>
         TryTakeLock(lockType, lockTimeout, cancellationToken) ?? throw RegisterTimeoutException();

#pragma warning disable CA1068 // Passing cancellation token around is standard practice in modern .NET while the timeout overrides are very rarely used. We don't want to force the common case to use named parameters.
      LockDisposer TakeLockWhen(Func<bool> condition, LockType lockType, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TryTakeLockWhen(condition, lockType, cancellationToken, waitTimeout, lockTimeout) ?? throw new AwaitingConditionTimeoutException();

      LockDisposer? TryTakeLock(LockType lockType, LockTimeout? timeout = null, CancellationToken cancellationToken = default) =>
         _monitor.TryTakeLock(timeout ?? LockTimeout, cancellationToken) ? LockFor(lockType) : null;

      LockDisposer? TryTakeLockWhen(Func<bool> condition, LockType lockType, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
#pragma warning restore CA1068
      {
         var effectiveWaitTimeout = waitTimeout ?? WaitTimeout;
         var effectiveLockTimeout = lockTimeout ?? LockTimeout;

         var waitStartedAt = DateTime.UtcNow;

         IDisposable takenLock = TakeLock(lockType, effectiveLockTimeout, cancellationToken);
         // When the token is cancelled, PulseAll wakes any thread blocked in Monitor.Wait so it can observe the cancellation.
         var wakeWaitingThreadsOnCancellation = cancellationToken.CanBeCanceled
                                                   ? cancellationToken.Register(_monitor.AcquireLockAndNotifyWaitingThreads)
                                                   : default(CancellationTokenRegistration);
         try
         {
            if(effectiveWaitTimeout.IsInfinite)
            {
               while(!condition())
               {
                  cancellationToken.ThrowIfCancellationRequested();
                  _monitor.ReleaseLockAndReacquireItOnPulseOrTimeout(WaitTimeout.Infinite);
               }
            } else
            {
               while(!condition())
               {
                  cancellationToken.ThrowIfCancellationRequested();
                  if(effectiveWaitTimeout.IsExpired(waitStartedAt))
                  {
                     takenLock.Dispose();
                     return null;
                  }

                  _monitor.ReleaseLockAndReacquireItOnPulseOrTimeout(effectiveWaitTimeout.TimeRemaining(waitStartedAt));
               }
            }
         }
         catch
         {
            ReleaseLock();
            throw;
         }
         finally
         {
            wakeWaitingThreadsOnCancellation.Unregister(); //Dispose would cause a deadlock in certain situations (test verified) so we use Unregister.
         }

         return LockFor(lockType);
      }

      void ReleaseLock()
      {
         UpdateAnyRegisteredTimeoutExceptions();
         _monitor.ReleaseLock();
      }

      IReadOnlyList<TakeMonitorLockTimeoutException> _timeOutExceptionsOnOtherThreads = new List<TakeMonitorLockTimeoutException>();

      Exception RegisterTimeoutException()
      {
         lock(_timeoutLock)
         {
            var exception = new TakeMonitorLockTimeoutException(LockTimeout, _stackTraceFetchTimeout);
            _timeOutExceptionsOnOtherThreads = [.._timeOutExceptionsOnOtherThreads, exception];
            return exception;
         }
      }

      void UpdateAnyRegisteredTimeoutExceptions()
      {
         // ReSharper disable once InconsistentlySynchronizedField
         if(_timeOutExceptionsOnOtherThreads.Count > 0)
         {
            lock(_timeoutLock)
            {
               var stackTrace = new StackTrace(fNeedFileInfo: true);
               foreach(var exception in _timeOutExceptionsOnOtherThreads)
               {
                  exception.SetBlockingThreadsDisposeStackTrace(stackTrace);
               }

               Interlocked.Exchange(ref _timeOutExceptionsOnOtherThreads, new List<TakeMonitorLockTimeoutException>());
            }
         }
      }

      enum LockType
      {
         Read = 0,
         Update = 1
      }

      class ThinMonitorWrapper
      {
         readonly object _lockObject = new();
         long _contentionCount = 0;
         public long ContentionCount => _contentionCount;

         static readonly TimeSpan CancellationPollingInterval = TimeSpan.FromSeconds(1);

         public bool TryTakeLock(LockTimeout timeout, CancellationToken cancellationToken = default)
         {
            if(Monitor.TryEnter(_lockObject)) return true; //This will never block, calling it is essentially free and allows us to collect contention statistics
            Interlocked.Increment(ref _contentionCount);
            if(!cancellationToken.CanBeCanceled) return Monitor.TryEnter(_lockObject, timeout);

            // Poll with short intervals so we can respond to cancellation
            var deadline = DateTime.UtcNow + timeout.ToTimeSpan();
            while(true)
            {
               cancellationToken.ThrowIfCancellationRequested();
               var remaining = deadline - DateTime.UtcNow;
               if(remaining <= TimeSpan.Zero) return false;
               if(Monitor.TryEnter(_lockObject, remaining < CancellationPollingInterval ? remaining : CancellationPollingInterval)) return true;
            }
         }

         public void ReleaseLockAndReacquireItOnPulseOrTimeout(WaitTimeout timeout) => Monitor.Wait(_lockObject, timeout);

         public void ReleaseLock() => Monitor.Exit(_lockObject);

         public void NotifyWaitingThreadsAboutUpdates() => Monitor.PulseAll(_lockObject); //All threads blocked on Monitor.Wait for our _lockObject will now try and reacquire the lock

         public void AcquireLockAndNotifyWaitingThreads()
         {
            lock(_lockObject) Monitor.PulseAll(_lockObject);
         }
      }
   }
}
