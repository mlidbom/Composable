using System;
using Void.Plane;
using Void.Plane.Impl;
using Void.Time;

namespace Void.PlaneTime.Impl
{    
    [Obsolete(WarningMessages.InternalAndInheritanceOnly)]
    public class SimplePlaneTimePoint : SimplePlanePoint, IPlaneTimePoint
    {
        public DateTime DateTimeValue { get; private set; }
        public SimplePlaneTimePoint(IPlanePoint planePosition, ITimePoint timePosition):base(planePosition)
        {
            DateTimeValue = timePosition.DateTimeValue;
        }

        public ITimePoint TimePosition { get { return this; } }
    }
}