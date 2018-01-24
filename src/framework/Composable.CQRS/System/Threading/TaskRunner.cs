using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Composable.System.Linq;
using Composable.System.Threading.ResourceAccess;

namespace Composable.System.Threading
{
    public interface ITaskRunner
    {
        void MonitorAndCrashProcessIfTaskThrows(IEnumerable<Task> tasks);
        void MonitorAndCrashProcessIfTaskThrows(params Task[] tasks);
        void RunAndCrashProcessIfTaskThrows(IEnumerable<Action> tasks);
        void RunAndCrashProcessIfTaskThrows(params Action[] tasks);
    }

    public class TaskRunner : ITaskRunner, IDisposable
    {
        readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        readonly BlockingCollection<Action> _tasksQueue = new BlockingCollection<Action>();
        const int MaxRunningTasks = 5;
        readonly List<Thread> _taskRunnerThreads;

        public TaskRunner()
        {
            _taskRunnerThreads = 1.Through(MaxRunningTasks).Select(_ => new Thread(RunTaskWhenAwailable)).ToList();
            _taskRunnerThreads.ForEach(@this => @this.Start());
        }

        void RunTaskWhenAwailable()
        {
            while(!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var task = _tasksQueue.Take();
                    task.Invoke();
                }
                catch(Exception exception) when(exception is OperationCanceledException || exception is ThreadInterruptedException || exception is ThreadAbortException)
                {
                    return;
                }
            }
        }

        public void MonitorAndCrashProcessIfTaskThrows(IEnumerable<Task> tasks) => MonitorAndCrashProcessIfTaskThrows(tasks.ToArray());
        public void MonitorAndCrashProcessIfTaskThrows(params Task[] tasks) => tasks.ForEach(ThrowExceptionOnBackgroundThreadIfTaskFails);

        static void ThrowExceptionOnBackgroundThreadIfTaskFails(Task task) => task.ContinueWith(ThrowExceptionOnNewThreadSoThatProcessCrashesInsteadOfThisFailureGoingIgnoredAsIsTheDefaultBehaviorForTasks, TaskContinuationOptions.OnlyOnFaulted);
        static void ThrowExceptionOnNewThreadSoThatProcessCrashesInsteadOfThisFailureGoingIgnoredAsIsTheDefaultBehaviorForTasks(Task faultedTask) => new Thread(() => throw new Exception("Unhandled exception occured in background task", faultedTask.Exception)).Start();

        public void RunAndCrashProcessIfTaskThrows(IEnumerable<Action> tasks) => RunAndCrashProcessIfTaskThrows(tasks.ToArray());
        public void RunAndCrashProcessIfTaskThrows(params Action[] tasks) => tasks.ForEach(EnqueueAndCrashProcessIfTaskFails);

        void EnqueueAndCrashProcessIfTaskFails(Action action) => _tasksQueue.Add(action);

        public void Dispose()
        {
            _taskRunnerThreads.ForEach(thread => thread.InterruptAndJoin());
        }
    }
}
