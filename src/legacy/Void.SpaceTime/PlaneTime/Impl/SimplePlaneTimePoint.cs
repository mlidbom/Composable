#region usings

using System;
using Composable.SpaceTime.Plane;
using Composable.SpaceTime.Plane.Impl;
using Composable.SpaceTime.Time;

#endregion

namespace Composable.SpaceTime.PlaneTime.Impl
{
    [Obsolete(WarningMessages.InternalAndInheritanceOnly)]
    internal class SimplePlaneTimePoint : SimplePlanePoint, IPlaneTimePoint
    {
        private readonly DateTime _dateTimeValue;

        public DateTime AsDateTime()
        {
            return _dateTimeValue;
        }

        public SimplePlaneTimePoint(IPlanePoint planePosition, ITimePoint timePosition) : base(planePosition)
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