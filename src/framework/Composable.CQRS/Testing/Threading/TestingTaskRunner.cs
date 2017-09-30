using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.System.Linq;

namespace Composable.Testing.Threading
{
    ///<summary>
    /// Runs and monitors tasks on background threads.
    /// Throws <see cref="AggregateException"/> on dispose if any throw exceptions or do not complete within timeout. </summary>
    public class TestingTaskRunner : IDisposable
    {
        readonly List<Task> _tasks = new List<Task>();
        readonly TimeSpan _timeout;

        public static TestingTaskRunner WithTimeout(TimeSpan timeout) { return new TestingTaskRunner(timeout); }

        public TestingTaskRunner(TimeSpan timeout) => _timeout = timeout;

        public void Monitor(IEnumerable<Task> tasks) => Monitor(tasks.ToArray());
        public void Monitor(params Task[] task) => _tasks.AddRange(task);

        public void RunTimes(int times, Func<Task> task) => Monitor(1.Through(times).Select(index => task()));
        public void RunTimes(int times, Func<int, Task> task) => Monitor(1.Through(times).Select(task));

        public void Run(IEnumerable<Action> tasks) => Run(tasks.ToArray());
        public void Run(params Action[] tasks) => tasks.ForEach(task => _tasks.Add(Task.Run(task)));
        public void RunTimes(int times, Action task) => Run(1.Through(times).Select(index => task));
        public void RunTimes(int times, Action<int> task) => Run(1.Through(times).Select<int, Action>(index => () => task(index)));

        public void Dispose()
        {
            if(!Task.WaitAll(_tasks.ToArray(), timeout: _timeout))
            {
                var exceptions = _tasks.Where(@this => @this.IsFaulted)
                                       .Select(@this => @this.Exception)
                                       .ToList();

                if(exceptions.Any())
                {
                    throw new AggregateException($"Tasks failed to complete within timeout {_timeout} and there were exceptions in tasks", exceptions);
                }

                throw new AggregateException($"Tasks failed to completed within timeout: {_timeout}");
            }
        }
    }
}
