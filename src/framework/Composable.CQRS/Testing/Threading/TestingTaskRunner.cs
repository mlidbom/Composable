using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public void Monitor(params Task[] task) => _tasks.AddRange(task);

        public void Run(Action task) => _tasks.Add(Task.Run(task));

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
