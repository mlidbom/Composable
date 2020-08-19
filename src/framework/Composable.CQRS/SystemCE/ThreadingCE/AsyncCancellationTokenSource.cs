using System;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.SystemCE.ThreadingCE
{
    //Hack to implement the suggested framework fix from here: https://github.com/dotnet/runtime/issues/23405 so that calling cancel on a CancellationTokenSource does not call registrations synchronously.
    sealed class AsyncCancellationTokenSource : IDisposable
    {
        static readonly Func<CancellationTokenSource, IEnumerable?> GetCallbackPartitionsAsObject = CreateCallbackPartitionsAccessor();
        readonly CancellationTokenSource _source;

        public AsyncCancellationTokenSource() => _source = new CancellationTokenSource();
        public AsyncCancellationTokenSource(TimeSpan delay) => _source = new CancellationTokenSource(delay);
        public AsyncCancellationTokenSource(int millisecondsDelay) => _source = new CancellationTokenSource(millisecondsDelay);

        bool HasOrHasHadCallbacks => GetCallbackPartitionsAsObject(_source) != null;

        public CancellationToken Token => _source.Token;

        public bool IsCancellationRequested => _source.IsCancellationRequested;

        static readonly string CancelAsyncTaskName = $"{nameof(AsyncCancellationTokenSource)}_{nameof(CancelAsync)}";
        public Task CancelAsync()
        {
            if(HasOrHasHadCallbacks)
            {
                return TaskCE.Run(CancelAsyncTaskName, () => _source.Cancel());
            } else
            {
                _source.Cancel();
                return Task.CompletedTask;
            }
        }

        public void CancelAfter(TimeSpan delay) => _source.CancelAfter(delay);

        public void CancelAfter(int millisecondsDelay) => _source.CancelAfter(millisecondsDelay);

        public void Dispose() => _source.Dispose();

        static Func<CancellationTokenSource, IEnumerable> CreateCallbackPartitionsAccessor()
        {
            FieldInfo callbackPartitionsField = typeof(CancellationTokenSource).GetField("_callbackPartitions", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                             ?? throw new Exception("Failed to find the internal field:_callbackPartitions. You may be running a different version of .Net than this hack supports.");

            var cancellationTokenSource = Expression.Parameter(typeof(CancellationTokenSource), "cancellationTokenSource");

            return Expression.Lambda<Func<CancellationTokenSource, IEnumerable>>(
                Expression.Convert(
                    Expression.Field(cancellationTokenSource, callbackPartitionsField),
                    typeof(IEnumerable)),
                cancellationTokenSource).Compile();
        }
    }
}
