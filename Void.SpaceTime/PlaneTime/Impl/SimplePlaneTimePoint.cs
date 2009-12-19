using System;
using Void.Plane;
using Void.Plane.Impl;
using Void.Time;

namespace Void.PlaneTime.Impl
{
    public class SimplePlaneTimePoint : SimplePlanePoint, IPlaneTimePoint
    {
        public ITimePoint TimeCoordinate { get; private set; }
        
        //Hide leaking implementation detail with explicit implementation
        DateTime ITimePoint.AsDateTime() { return TimeCoordinate.AsDateTime(); }

        public SimplePlaneTimePoint(IPlanePoint spacePosition, ITimePoint timePosition):base(spacePosition)
        {
            TimeCoordinate = timePosition.TimeCoordinate;
        }        
    }
}