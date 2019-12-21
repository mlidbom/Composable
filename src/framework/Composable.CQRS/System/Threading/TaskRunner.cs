using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Composable.System.Linq;
using Composable.System.Reflection;
using Composable.System.Threading.ResourceAccess;
using JetBrains.Annotations;

namespace Composable.System.Threading
{
    interface ITaskRunner
    {
        void MonitorAndCrashProcessIfTaskThrows(IEnumerable<Task> tasks);
        void MonitorAndCrashProcessIfTaskThrows(params Task[] tasks);
        void RunAndCrashProcessIfTaskThrows(IEnumerable<Action> tasks);
        void RunAndCrashProcessIfTaskThrows(params Action[] tasks);
    }

    [UsedImplicitly] class TaskRunner : ITaskRunner, IDisposable
    {
        readonly ITaskRunner _inner;
        public TaskRunner()
        {
            //var physicalCores = Math.Max(Environment.ProcessorCount, 2) / 2;
            // ReSharper disable once UnusedVariable
            //var maxParallelTasks = Math.Max(physicalCores, 8);
            //_inner = new ManualThreadsRunner(maxParallelTasks);
            //_inner = new ThrottledSystemTasksRunner(maxParallelTasks);
            // ReSharper disable once ArrangeConstructorOrDestructorBody
            _inner = new SystemTasksRunner();
        }

        public void MonitorAndCrashProcessIfTaskThrows(IEnumerable<Task> tasks) => _inner.MonitorAndCrashProcessIfTaskThrows(tasks);
        public void MonitorAndCrashProcessIfTaskThrows(params Task[] tasks) => _inner.MonitorAndCrashProcessIfTaskThrows(tasks);
        public void RunAndCrashProcessIfTaskThrows(IEnumerable<Action> tasks) => _inner.RunAndCrashProcessIfTaskThrows(tasks);
        public void RunAndCrashProcessIfTaskThrows(params Action[] tasks) => _inner.RunAndCrashProcessIfTaskThrows(tasks);
        public void Dispose() => (_inner as IDisposable)?.Dispose();


        abstract class RunnerBase : ITaskRunner
        {
            public void MonitorAndCrashProcessIfTaskThrows(IEnumerable<Task> tasks) => MonitorAndCrashProcessIfTaskThrows(tasks.ToArray());
            public void MonitorAndCrashProcessIfTaskThrows(params Task[] tasks) => tasks.ForEach(ThrowExceptionOnBackgroundThreadIfTaskFails);

            static void ThrowExceptionOnBackgroundThreadIfTaskFails(Task task) => task.ContinueWith(ThrowExceptionOnNewThreadSoThatProcessCrashesInsteadOfThisFailureGoingIgnoredAsIsTheDefaultBehaviorForTasks, TaskContinuationOptions.OnlyOnFaulted);
            static void ThrowExceptionOnNewThreadSoThatProcessCrashesInsteadOfThisFailureGoingIgnoredAsIsTheDefaultBehaviorForTasks(Task faultedTask) => new Thread(() => throw new Exception("Unhandled exception occured in background task", faultedTask.Exception)).Start();

            public void RunAndCrashProcessIfTaskThrows(IEnumerable<Action> tasks) => RunAndCrashProcessIfTaskThrows(tasks.ToArray());
            public void RunAndCrashProcessIfTaskThrows(params Action[] tasks) => tasks.ForEach(action => EnqueueWrappedTask(
                                                                                                   () =>
                                                                                                   {
                                                                                                       try
                                                                                                       {
                                                                                                           action();
                                                                                                       }
                                                                                                       catch(Exception exception)
                                                                                                       {
                                                                                                           new Thread(() => throw new Exception("Unhandled exception occured in background task", exception)).Start();
                                                                                                       }
                                                                                                   }));

            protected abstract void EnqueueWrappedTask(Action action);
        }

        // ReSharper disable once UnusedMember.Local
        class SystemTasksRunner : RunnerBase, IDisposable
        {
            readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

            protected override void EnqueueWrappedTask(Action action) => Task.Run(action, _cancellationTokenSource.Token);

            public void Dispose()
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedType.Local
        class ThrottledSystemTasksRunner : RunnerBase, IDisposable
        {
            readonly int _maxRunningTasks;

            readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
            readonly AwaitableOptimizedThreadShared<State> _state = new AwaitableOptimizedThreadShared<State>(new State());
            readonly Thread _dispatcherThread;

            public ThrottledSystemTasksRunner(int maxRunningTasks)
            {
                _dispatcherThread = new Thread(DispatcherThread){Name = $"{typeof(ThrottledSystemTasksRunner).GetFullNameCompilable()}_{nameof(DispatcherThread)}"};
                _maxRunningTasks = maxRunningTasks;
                _dispatcherThread.Start();
            }

            void DispatcherThread()
            {
                try
                {
                    while(true)
                    {
                        var task = _state.UpdateWhen(state => state.QueuedTasks.Count > 0 && state.RunningTasks < _maxRunningTasks, state =>
                        {
                            state.RunningTasks++;
                            return state.QueuedTasks.Dequeue();
                        });

                        void RunTask()
                        {
                            task();
                            _state.Update(state => state.RunningTasks--);
                        }

                        Task.Factory.StartNew(RunTask, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
                    }
                }
                catch(Exception exception) when(exception is OperationCanceledException || exception is ThreadInterruptedException || exception is ThreadAbortException)
                {}
            }
            protected override void EnqueueWrappedTask(Action action) => _state.Update(state => state.QueuedTasks.Enqueue(action));

            class State
            {
                internal readonly Queue<Action> QueuedTasks = new Queue<Action>();
                internal int RunningTasks;
            }

            public void Dispose()
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _dispatcherThread.InterruptAndJoin();
            }
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedType.Local
        class ManualThreadsRunner : RunnerBase, IDisposable
        {
            readonly BlockingCollection<Action> _tasksQueue = new BlockingCollection<Action>();
            readonly List<Thread> _taskRunnerThreads;
            readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

            public ManualThreadsRunner(int maxRunningTasks)
            {
                _taskRunnerThreads = 1.Through(maxRunningTasks).Select(_ => new Thread(RunTaskWhenAvailable)).ToList();
                _taskRunnerThreads.ForEach(@this => @this.Start());
            }

            void RunTaskWhenAvailable()
            {
                try
                {
                    while(true)
                    {
                        var task = _tasksQueue.Take(_cancellationTokenSource.Token);
                        task.Invoke();
                    }
                }
                catch(Exception exception) when(exception is OperationCanceledException || exception is ThreadInterruptedException || exception is ThreadAbortException)
                {}
            }

            protected override void EnqueueWrappedTask(Action action)
            {
                _tasksQueue.Add(action);
            }

            public void Dispose()
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _taskRunnerThreads.ForEach(thread => thread.InterruptAndJoin());
                _tasksQueue.Dispose();
            }
        }
    }
}
