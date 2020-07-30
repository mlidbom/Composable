using System;
using System.Threading;
using Composable.Logging;
using JetBrains.Annotations;

namespace Composable.SystemCE.ThreadingCE
{
    interface ITaskRunner
    {
        //Urgent: We cannot just ignore exceptions because we log them. Maybe calling component should be notified about them somehow and get to decide what to do?. Maybe return a Task that calling code is responsible for checking the result of sooner or later?
        //Urgent: Check out: TaskScheduler.UnobservedTaskException and if we should use it. Perhaps in EndpointHost.
        void RunAndSurfaceExceptions(string taskName, Action task);
    }

    [UsedImplicitly] class TaskRunner : ITaskRunner, IDisposable
    {
        public void RunAndSurfaceExceptions(string taskName, Action task)
        {
            TaskCE.Run(taskName, _cancellationTokenSource.Token, () =>
                     {
                         try
                         {
                             task();
                         }
                         catch(Exception exception)
                         {
                             this.Log().Error(exception, "Exception thrown on background thread. ");
                         }
                     });
        }

        readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public void Dispose() => _cancellationTokenSource.Dispose();
    }
}
