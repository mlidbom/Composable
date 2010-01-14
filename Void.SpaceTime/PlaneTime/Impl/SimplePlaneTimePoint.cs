using System;
using Void.Plane;
using Void.Plane.Impl;
using Void.Time;

namespace Void.PlaneTime.Impl
{    
    [Obsolete(WarningMessages.InternalAndInheritanceOnly)]
    internal class SimplePlaneTimePoint : SimplePlanePoint, IPlaneTimePoint
    {
        private readonly DateTime _dateTimeValue;

        public DateTime AsDateTime()
        {
            return _dateTimeValue;
        }

        public SimplePlaneTimePoint(IPlanePoint planePosition, ITimePoint timePosition):base(planePosition)
        {
            _dateTimeValue = timePosition.AsDateTime();
        }

        public ITimePoint ProjectAt(ITimePoint targetTime)
        {
            return new SimplePlaneTimePoint(PlanePosition, targetTime);
        }

        public ITimePoint TimePosition { get { return this; } }
    }
}