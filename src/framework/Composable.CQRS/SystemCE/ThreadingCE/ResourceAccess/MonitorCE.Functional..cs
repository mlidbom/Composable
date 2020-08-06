using System;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    partial class MonitorCE
    {
        public void Update(Action action)
        {
            using(EnterNotifyAllLock()) action();
        }

        public T Update<T>(Func<T> func)
        {
            using(EnterNotifyAllLock()) return func();
        }

        public TReturn Read<TReturn>(Func<TReturn> func)
        {
            using(EnterLock()) return func();
        }
    }
}
