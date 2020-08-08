using System;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable MethodSupportsCancellation
#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler. We use our own factory instance that already specifies the Scheduler

namespace Composable.SystemCE.ThreadingCE.TasksCE
{
    static partial class TaskCE
    {
        ///<summary>Here just in order to collect all run methods here so that all task execution comes through this class and we can easily find them all and review them for why they don't pass a name for easier debugging. Any call to  <see cref="Task.Run(System.Action)"/> elsewhere is known to be a mistake to be replaced with a call to this method.</summary>
        public static Task Run(Action action) => Task.Run(action);

        public static Task Run(Func<Task> asyncAction) => Task.Run(asyncAction);

        public static Task RunOnDedicatedThread(Action action) => DefaultSchedulerDenyChildAttachTaskFactory.StartNew(action, TaskCreationOptions.LongRunning);

        ///<summary>Using this instead of Task.Run can help debugging very much by providing a "name" for the task in the debugger views for Tasks</summary>
        public static Task Run(string name, Action action) => Run(name, action, CancellationToken.None);
        public static Task Run(string name, Action action, CancellationToken cancellationToken) => DefaultSchedulerDenyChildAttachTaskFactory.StartNew(_ => action(), name, cancellationToken);

        public static Task Run(string name, Func<Task> action) => Run(name, action, CancellationToken.None);
        public static Task Run(string name, Func<Task> action, CancellationToken cancellationToken) => DefaultSchedulerDenyChildAttachTaskFactory.StartNew(async _ => await action().NoMarshalling(), name, cancellationToken);

        ///<summary>Here just in order to collect all run methods here so that all task execution comes through this class and we can easily find them all and review them for why they don't pass a name for easier debugging. Any call to  <see cref="Task.Run(System.Action)"/> elsewhere is known to be a mistake to be replaced with a call to this method.</summary>
        public static Task<TResult> Run<TResult>(Func<TResult> function) => Task.Run(function);
    }
}
