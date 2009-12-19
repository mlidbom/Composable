using System;
using Void.Plane;
using Void.Plane.Impl;
using Void.Time;

namespace Void.PlaneTime.Impl
{
#pragma warning disable 612,618
    [Obsolete(WarningMessages.InternalAndInheritanceOnly)]
    public class SimplePlaneTimeInterval : SimplePlanePositioned, IPlaneTimeInterval
#pragma warning restore 612,618
    {
        public SimplePlaneTimeInterval(IPlanePoint position, ITimeInterval time) : base(position)
        {
            Duration = time.Duration;
        }

        public ITimePoint TimePosition { get; private set; }
        public IDuration Duration { get; set; }
    }
}