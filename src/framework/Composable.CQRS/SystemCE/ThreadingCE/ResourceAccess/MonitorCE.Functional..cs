using System;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    public partial class MonitorCE
    {
        public delegate T OutParamDelegate<T>(out T outParam);

        public TReturn Read<TReturn>(Func<TReturn> func)
        {
            using(EnterReadLock()) return func();
        }

        public void Update(Action action)
        {
            using(EnterUpdateLock()) action();
        }

        public T Update<T>(Func<T> func)
        {
            using(EnterUpdateLock()) return func();
        }

        public T Update<T>(OutParamDelegate<T> func, out T outParam)
        {
            using(EnterUpdateLock()) return func(out outParam);
        }
    }
}
