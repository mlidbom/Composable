using System;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    partial class MonitorCE
    {
        internal void Update(Action action)
        {
            using(EnterNotifyAllUpdateLock()) action();
        }

        internal T Update<T>(Func<T> func)
        {
            using(EnterNotifyAllUpdateLock()) return func();
        }

        internal TReturn Read<TReturn>(Func<TReturn> func)
        {
            using(EnterLock()) return func();
        }
    }
}
