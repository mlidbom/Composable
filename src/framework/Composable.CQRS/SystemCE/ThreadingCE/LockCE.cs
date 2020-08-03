using System;

namespace Composable.SystemCE.ThreadingCE
{

#pragma warning disable CA2002 // Do not lock on objects with weak identity
    internal class LockCE
    {
        internal void Run(Action action)
        {
            lock (this)
            {
                action();
            }
        }

        internal TResult Run<TResult>(Func<TResult> func)
        {
            lock(this)
            {
                return func();
            }
        }
    }
}
