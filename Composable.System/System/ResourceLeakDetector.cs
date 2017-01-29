using System;

namespace Composable.System
{
    public class ResourceLeakDetector : IDisposable
    {
        public ResourceLeakDetector() { ReservationCallStack = Environment.StackTrace; }
        public string ReservationCallStack { get; }

        bool _disposed;
        public void Dispose() { _disposed = true; }

        public void FinalizerCalled()
        {
            if(_disposed)
                return;

            throw new ResourceWasLeakedException(ReservationCallStack);
        }
    }

    public class ResourceWasLeakedException : Exception
    {
        public ResourceWasLeakedException(string reservationCallStack):base(FormatMessage(reservationCallStack))
        {
            
        }
        static string FormatMessage(string reservationCallStack) { return $@"Reservation call stack: {reservationCallStack}"; }
    }
}