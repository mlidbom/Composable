using System;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable MethodSupportsCancellation

namespace Composable.SystemCE.ThreadingCE.TasksCE
{
    static partial class TaskCE
    {
        //This mirrors the options used internally by Task.Run. We just want to add the ability to specify a name via the state parameter which will then be shown in the debugger, thus making debugging significantly easier.
        static readonly TaskFactory DefaultSchedulerDenyChildAttachTaskFactory = new TaskFactory(CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskContinuationOptions.None, TaskScheduler.Default);
    }
}
