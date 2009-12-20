using System;
using Void.Plane;
using Void.Plane.Impl;
using Void.Time;

namespace Void.PlaneTime.Impl
{    
#pragma warning disable 612,618    
    [Obsolete(WarningMessages.InternalAndInheritanceOnly)]
    public class SimplePlaneTimePoint : SimplePlanePoint, IPlaneTimePoint
#pragma warning restore 612,618
    {
        public DateTime DateTimeValue { get; private set; }
        public SimplePlaneTimePoint(IPlanePoint planePosition, ITimePoint timePosition):base(planePosition)
        {
#pragma warning disable 612,618
            DateTimeValue = timePosition.DateTimeValue;
#pragma warning restore 612,618
        }

        public ITimePoint TimePosition { get { return this; } }
    }
}