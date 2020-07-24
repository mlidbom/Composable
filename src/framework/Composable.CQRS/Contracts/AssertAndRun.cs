using System;
using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Contracts
{
    public class AssertAndRun
    {
        readonly Action _assertion;
        public AssertAndRun(Action assertion) => _assertion = assertion;

        internal TResult Do<TResult>(Func<TResult> action)
        {
            _assertion();
            return action();
        }

        internal void Do(Action action)
        {
            _assertion();
            action();
        }

        internal async Task<TResult> DoAsync<TResult>(Func<Task<TResult>> action)
        {
            _assertion();
            return await action().NoMarshalling();
        }

        internal async Task DoAsync(Func<Task> action)
        {
            _assertion();
            await action().NoMarshalling();
        }

        internal void Assert() => _assertion();
    }
}
