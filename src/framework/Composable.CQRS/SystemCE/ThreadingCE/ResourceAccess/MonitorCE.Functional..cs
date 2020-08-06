using System;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    partial class MonitorCE
    {
        internal void Update(Action action)
        {
            using(EnterNotifyAllLock()) action();
        }

        internal T Update<T>(Func<T> func)
        {
            using(EnterNotifyAllLock()) return func();
        }

        internal TReturn Read<TReturn>(Func<TReturn> func)
        {
            using(EnterLock()) return func();
        }
    }
}
