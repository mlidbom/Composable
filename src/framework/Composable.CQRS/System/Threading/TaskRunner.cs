using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Composable.Logging;
using Composable.System.Linq;
using JetBrains.Annotations;

namespace Composable.System.Threading
{
    interface ITaskRunner
    {
        //Urgent: We cannot just ignore exceptions because we log them. Maybe calling component should be notified about them somehow and get to decide what to do?. Maybe return a Task that calling code is responsible for checking the result of sooner or later?
        sealed void RunAndSurfaceExceptions(params Action[] tasks) => RunAndSurfaceExceptions((IEnumerable<Action>)tasks);
        void RunAndSurfaceExceptions(IEnumerable<Action> tasks);
    }

    [UsedImplicitly] class TaskRunner : ITaskRunner, IDisposable
    {
        public void RunAndSurfaceExceptions(IEnumerable<Action> tasks) => tasks.ForEach(action =>
        {
            Task.Run(() =>
                     {
                         try
                         {
                             action();
                         }
                         catch(Exception exception)
                         {
                             this.Log().Error(exception, "Exception thrown on background thread. ");
                         }
                     },
                     _cancellationTokenSource.Token);
        });

        readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public void Dispose() => _cancellationTokenSource.Dispose();
    }
}
