namespace Compze.Threading._internal;

///<summary>Bridges <see cref="IAwaitableCriticalSection"/>'s blocking condition waits into async callers. The name says where<br/>
/// the blocking wait parks — a dedicated thread, because a condition wait can span seconds and parking thread-pool threads<br/>
/// for long waits starves the pool, the collapse mode async code exists to prevent.</summary>
///<remarks>In <c>_internal</c> deliberately: this belongs on the public <see cref="IAwaitableCriticalSection"/> surface<br/>
/// eventually, but the API is not declared ready for publishing; Tessaging consumes it meanwhile.</remarks>
///<remarks>Compze standardizes this kind of dispatch on <c>TaskCE</c> (<c>Compze.Internals.SystemCE</c>), but that project is<br/>
/// built on top of this one, so the bridge dispatches through the BCL directly, replicating <c>TaskCE</c>'s choices:<br/>
/// <see cref="TaskCreationOptions.LongRunning"/> for the dedicated thread, <see cref="TaskCreationOptions.DenyChildAttach"/><br/>
/// and <see cref="TaskScheduler.Default"/> so no ambient scheduler or child-task attachment changes where the wait runs.</remarks>
static class IAwaitableCriticalSectionCE
{
   static readonly TaskFactory DefaultSchedulerDenyChildAttachTaskFactory = new(CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskContinuationOptions.None, TaskScheduler.Default);

   extension(IAwaitableCriticalSection @this)
   {
#pragma warning disable CA1068 // Passing cancellation token around is standard practice in modern .NET while the timeout overrides are very rarely used. We don't want to force the common case to use named parameters.
      ///<summary><see cref="IAwaitableCriticalSection.TryAwait"/> for async callers: awaits until <paramref name="condition"/><br/>
      /// returns true or <paramref name="waitTimeout"/> expires — false on timeout, else true. The name says where a wait that<br/>
      /// must actually block parks: a dedicated thread, which costs a thread but never the thread pool's health — the choice<br/>
      /// for a wait that can be long.</summary>
      ///<remarks><paramref name="condition"/> is first evaluated inline on the calling thread, under a read lock: in the<br/>
      /// common case it already holds, and awaiting must not cost a thread to discover that — no thread is parked unless the<br/>
      /// wait must actually block. The calling thread blocks only for that lock acquisition, bounded by lock contention, never<br/>
      /// by the condition wait. So <paramref name="condition"/> runs on the calling thread first and on the wait's dedicated<br/>
      /// thread thereafter, re-evaluated on every update-lock release until it holds — it must tolerate any thread and<br/>
      /// repeated evaluation, and a <paramref name="condition"/> that throws propagates out of the wait immediately.</remarks>
#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler The factory was created with TaskScheduler.Default.
      internal async Task<bool> TryAwaitOnDedicatedThreadAsync(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
      {
         if(@this.Read(condition, cancellationToken, lockTimeout)) return true;

         return await DefaultSchedulerDenyChildAttachTaskFactory.StartNew(() => @this.TryAwait(condition, cancellationToken, waitTimeout, lockTimeout), TaskCreationOptions.LongRunning).ConfigureAwait(false);
      }
#pragma warning restore CA2008
#pragma warning restore CA1068
   }
}
