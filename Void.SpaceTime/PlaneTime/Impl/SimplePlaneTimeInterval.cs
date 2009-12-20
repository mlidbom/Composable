using System;
using Void.Plane;
using Void.Plane.Impl;
using Void.Time;

namespace Void.PlaneTime.Impl
{
    [Obsolete(WarningMessages.InternalAndInheritanceOnly)]
    public class SimplePlaneTimeInterval : SimplePlanePositioned, IPlaneTimeInterval
    {
        public SimplePlaneTimeInterval(IPlanePoint position, IDuration time) : base(position)
        {
            Duration = time;
        }

        public ITimePoint TimePosition { get; private set; }
        public IDuration Duration { get; set; }
        public TimeSpan TimeSpanValue { get { return Duration.TimeSpanValue; } }
    }
}