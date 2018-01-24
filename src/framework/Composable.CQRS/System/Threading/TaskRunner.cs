using System;
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
        readonly AwaitableOptimizedThreadShared<State> _state = new AwaitableOptimizedThreadShared<State>(new State());
        readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        const int MaxRunningTasks = 20;
        readonly Thread _taskDispatcherThread;
        public TaskRunner()
        {
            _taskDispatcherThread = new Thread(DispatchTasks){Name = $"{nameof(TaskRunner)}_{nameof(DispatchTasks)}"};
            _taskDispatcherThread.Start();
        }

        void DispatchTasks()
        {
            while(!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    _state.ExecuteWithExclusiveAccessWhen(state => state.RunningTasks < MaxRunningTasks && state.QueuedTasks.Count > 0,
                                                          state =>
                                                          {
                                                              var actionToRun = state.QueuedTasks.Dequeue();
                                                              var startedTask = Task.Factory.StartNew(actionToRun);
                                                              startedTask.ContinueWith(completedTask => _state.Update(innerState => innerState.RunningTasks--));
                                                              MonitorAndCrashProcessIfTaskThrows(startedTask);
                                                              state.RunningTasks++;
                                                          });
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

        void EnqueueAndCrashProcessIfTaskFails(Action action) => _state.Update(state => state.QueuedTasks.Enqueue(action));

        class State
        {
            public int RunningTasks = 0;
            public readonly Queue<Action> QueuedTasks = new Queue<Action>();
        }

        public void Dispose()
        {
            _taskDispatcherThread.InterruptAndJoin();
        }
    }
}
