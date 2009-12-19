using System;
using Void.Plane;
using Void.Plane.Impl;
using Void.Time;

namespace Void.PlaneTime.Impl
{
    public class SimplePlaneTimePoint : SimplePlanePoint, IPlaneTimePoint
    {
        public DateTime DateTimeValue { get; private set; }
        public SimplePlaneTimePoint(IPlanePoint spacePosition, ITimePoint timePosition):base(spacePosition)
        {
            DateTimeValue = timePosition.DateTimeValue;
        }        
    }
}