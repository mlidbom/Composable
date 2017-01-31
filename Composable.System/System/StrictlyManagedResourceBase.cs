using System;

namespace Composable.System
{
    public abstract class StrictlyManagedResourceBase : IDisposable
    {
        protected StrictlyManagedResourceBase() { ReservationCallStack = Environment.StackTrace; }
        string ReservationCallStack { get; }

        bool _disposed;
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (!_disposed)
            {
                InternalDispose();
            }
            _disposed = true;
        }

        protected abstract void InternalDispose();

        ~StrictlyManagedResourceBase()
        {
            if(_disposed)
                return;

            throw new ResourceWasLeakedException(GetType(), ReservationCallStack);
        }
    }

    public class ResourceWasLeakedException : Exception
    {
        public ResourceWasLeakedException(Type instanceType, string reservationCallStack):base(FormatMessage(instanceType, reservationCallStack))
        {
            
        }
        static string FormatMessage(Type instanceType, string reservationCallStack) {
            return $@"User code failed to InternalDispose this instance of {instanceType.FullName}
Construction call stack: {reservationCallStack}"; }
    }
}