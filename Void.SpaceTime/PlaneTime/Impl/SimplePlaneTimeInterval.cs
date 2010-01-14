using System;
using Void.Plane;
using Void.Plane.Impl;
using Void.Time;

namespace Void.PlaneTime.Impl
{
    /// <summary>A simple property backed implementation of <see cref="IPlaneTimeInterval"/></summary>
    [Obsolete(WarningMessages.InternalAndInheritanceOnly)]
    internal class SimplePlaneTimeInterval : SimplePlanePositioned, IPlaneTimeInterval
    {
        /// <summary>Creates an instance at the specified <paramref name="position"/> and <paramref name="time"/></summary>
        public SimplePlaneTimeInterval(IPlanePoint position, IDuration time) : base(position)
        {
            Duration = time;
        }

        public ITimePoint TimePosition { get; private set; }
        public IDuration Duration { get; set; }

        public TimeSpan AsTimeSpan()
        {
            return Duration.AsTimeSpan();
        }
    }
}