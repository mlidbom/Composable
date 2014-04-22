using System;
using System.Diagnostics.Contracts;

namespace Composable
{
    public class DisposeAction : IDisposable
    {
        private readonly Action _action;

        public DisposeAction(Action action)
        {
            Contract.Requires(action != null);
            _action = action;
        }

        public void Dispose()
        {
            _action();
        }
    }
}