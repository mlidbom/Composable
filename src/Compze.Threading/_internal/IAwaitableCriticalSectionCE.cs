namespace Compze.Threading._internal;

///<summary>Bridges <see cref="IAwaitableCriticalSection"/>'s blocking condition waits into async callers. The caller chooses,<br/>
/// by name, which thread the blocking wait parks: a dedicated thread for a wait that can span seconds — parking thread-pool<br/>
/// threads for long waits starves the pool, the collapse mode async code exists to prevent — or a thread-pool thread for a<br/>
/// wait known to be brief.</summary>
///<remarks>In <c>_internal</c> deliberately: these belong on the public <see cref="IAwaitableCriticalSection"/> surface<br/>
/// eventually, but the API is not declared ready for publishing; Tessaging consumes them meanwhile.</remarks>
///<remarks>Compze standardizes this kind of dispatch on <c>TaskCE</c> (<c>Compze.Internals.SystemCE</c>), but that project is<br/>
/// built on top of this one, so these bridges dispatch through the BCL directly, replicating <c>TaskCE</c>'s choices:<br/>
/// <see cref="TaskCreationOptions.LongRunning"/> for the dedicated thread, <see cref="TaskCreationOptions.DenyChildAttach"/><br/>
/// and <see cref="TaskScheduler.Default"/> so no ambient scheduler or child-task attachment changes where the wait runs.</remarks>
static class IAwaitableCriticalSectionCE
{
   static readonly TaskFactory DefaultSchedulerDenyChildAttachTaskFactory = new(CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskContinuationOptions.None, TaskScheduler.Default);

   extension(IAwaitableCriticalSection @this)
   {
#pragma warning disable CA1068 // Passing cancellation token around is standard practice in modern .NET while the timeout overrides are very rarely used. We don't want to force the common case to use named parameters.
      ///<summary><see cref="IAwaitableCriticalSection.TryAwait"/> parked on a dedicated thread: awaits until<br/>
      /// <paramref name="condition"/> returns true or <paramref name="waitTimeout"/> expires — false on timeout, else true.<br/>
      /// The choice for a wait that can be long: the dedicated thread costs a thread, never the thread pool's health.</summary>
#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler The factory was created with TaskScheduler.Default.
      internal async Task<bool> TryAwaitOnDedicatedThreadAsync(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         await DefaultSchedulerDenyChildAttachTaskFactory.StartNew(() => @this.TryAwait(condition, cancellationToken, waitTimeout, lockTimeout), TaskCreationOptions.LongRunning).ConfigureAwait(false);

      ///<summary><see cref="IAwaitableCriticalSection.TryAwait"/> parked on a thread-pool thread: awaits until<br/>
      /// <paramref name="condition"/> returns true or <paramref name="waitTimeout"/> expires — false on timeout, else true.<br/>
      /// Only for a wait known to be brief: the parked thread is one of the pool's, and a pool full of parked waiters is a<br/>
      /// starved pool.</summary>
      internal async Task<bool> TryAwaitOnThreadPoolThreadAsync(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         await DefaultSchedulerDenyChildAttachTaskFactory.StartNew(() => @this.TryAwait(condition, cancellationToken, waitTimeout, lockTimeout)).ConfigureAwait(false);
#pragma warning restore CA2008
#pragma warning restore CA1068
   }
}
