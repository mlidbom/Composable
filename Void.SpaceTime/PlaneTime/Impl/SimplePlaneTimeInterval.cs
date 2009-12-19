using System;
using Void.Plane;
using Void.Plane.Impl;
using Void.Time;

namespace Void.PlaneTime.Impl
{
    public class SimplePlaneTimeInterval : SimplePlanePositioned, IPlaneTimeInterval
    {
        public SimplePlaneTimeInterval(IPlanePositioned position, ITimeInterval time) : base(position)
        {
            Duration = time.Duration;
        }

        public ITimePoint TimeCoordinate { get; private set; }
        public TimeSpan Duration { get; set; }
    }
}