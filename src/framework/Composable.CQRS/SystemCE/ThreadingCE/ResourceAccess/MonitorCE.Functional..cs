using System;
using System.Threading;

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

        public ReentrancyAllowedScope EnterReentrancyAllowedScope() => new ReentrancyAllowedScope(this);

        public sealed class ReentrancyAllowedScope : IDisposable
        {
            readonly MonitorCE _monitor;
            internal ReentrancyAllowedScope(MonitorCE monitor)
            {
                _monitor = monitor;
                AssertOwnsLock();
                _monitor._allowReentrancyIfGreaterThanZero++;
            }

            void AssertOwnsLock()
            {
                if(_monitor._ownerThread != Thread.CurrentThread.ManagedThreadId) throw new Exception("You must own the lock to enable or disable reentrancy.");
            }

            public void Dispose()
            {
                AssertOwnsLock();
                _monitor._allowReentrancyIfGreaterThanZero--;
            }
        }
    }
}
