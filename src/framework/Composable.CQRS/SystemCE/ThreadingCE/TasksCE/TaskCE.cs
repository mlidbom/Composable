using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Composable.SystemCE.ThreadingCE.TasksCE
{
    static partial class TaskCE
    {
        internal static TResult ResultUnwrappingException<TResult>(this Task<TResult> task)
        {
            try
            {
                return task.Result;
            }
            catch(AggregateException exception)
            {
                if(exception.InnerExceptions.Count == 1 && exception.InnerException != null)
                {
                    ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                } else
                {
                    throw;
                }
            }

            throw new Exception("Impossible!");
        }

        internal static void WaitUnwrappingException(this Task task)
        {
            try
            {
                task.Wait();
                return;
            }
            catch(AggregateException exception)
            {
                if(exception.InnerExceptions.Count == 1 && exception.InnerException != null)
                {
                    ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                } else
                {
                    throw;
                }
            }

            throw new Exception("Impossible!");
        }

        internal static Task ContinueAsynchronouslyOnDefaultScheduler(this Task @this, Action<Task> continuation, TaskContinuationOptions options = TaskContinuationOptions.RunContinuationsAsynchronously) => @this.ContinueWith(continuation, CancellationToken.None, options, TaskScheduler.Default);

        internal static Task ContinueAsynchronouslyOnDefaultScheduler<TResult>(this Task<TResult> @this, Action<Task<TResult>> continuation, TaskContinuationOptions options = TaskContinuationOptions.RunContinuationsAsynchronously) => @this.ContinueWith(continuation, CancellationToken.None, options, TaskScheduler.Default);

        public static Task StartOnDedicatedThread(Action action) => Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        ///<summary>Here just in order to collect all run methods here so that all task execution comes through this class and we can easily find them all and review them for why they don't pass a name for easier debugging. Any call to  <see cref="Task.Run(System.Action)"/> elsewhere is known to be a mistake to be replaced with a call to this method.</summary>
        public static Task Run(Action action) => Task.Run(action);

        public static Task Run(Func<Task> asyncAction) => Task.Run(asyncAction);

        ///<summary>Here just in order to collect all run methods here so that all task execution comes through this class and we can easily find them all and review them for why they don't pass a name for easier debugging. Any call to  <see cref="Task.Run(System.Action)"/> elsewhere is known to be a mistake to be replaced with a call to this method.</summary>
        public static Task<TResult> Run<TResult>(Func<TResult> function) => Task.Run(function);

        ///<summary>Using this instead of Task.Run can help debugging very much by providing a "name" for the task in the debugger views for Tasks</summary>
        public static Task Run(string name, Action action) => Run(name, action, CancellationToken.None);
        public static Task Run(string name, Action action, CancellationToken cancellationToken) => Task.Factory.StartNew(_ => action(), name, cancellationToken, TaskCreationOptions.None, TaskScheduler.Default);

        public static Task Run(string name, Func<Task> action) => Run(name, action, CancellationToken.None);
        public static Task Run(string name, Func<Task> action, CancellationToken cancellationToken) => Task.Factory.StartNew(async _ => await action().NoMarshalling(), name, cancellationToken, TaskCreationOptions.None, TaskScheduler.Default);

        static readonly object DummyObject = new object();
        static readonly Task<object> CompletedObjectTask = Task.FromResult(DummyObject);

        internal static Func<TParam, object> AsFunc<TParam>(this Action<TParam> @this) =>
            param =>
            {
                @this(param);
                return DummyObject;
            };

        internal static Func<TParam, Task<object>> AsFunc<TParam>(this Func<TParam, Task> @this) =>
            param =>
            {
                @this(param);
                return CompletedObjectTask;
            };

        internal static Func<object> AsFunc(this Action @this) =>
            () =>
            {
                @this();
                return DummyObject;
            };
    }
}
