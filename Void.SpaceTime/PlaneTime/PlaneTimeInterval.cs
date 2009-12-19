using System;
using Void.Plane;
using Void.PlaneTime.Impl;
using Void.Time;

namespace Void.PlaneTime
{
    public static class PlaneTimeInterval
    {
        public static IPlaneTimeInterval MoveTo(IPlaneTimeInterval me, ITimePoint targetTime)
        {
            return new SimplePlaneTimeInterval(me, me.MoveTo(targetTime));
        }

        public static IPlaneTimeInterval MoveTo(IPlaneTimeInterval me, IPlanePoint targetPosition)
        {
            throw new NotImplementedException();
        }
    }
}