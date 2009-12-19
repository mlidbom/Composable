using System;
using Void.Plane;
using Void.PlaneTime.Impl;
using Void.Time;

namespace Void.PlaneTime
{
    public static class PlaneTimeInterval
    {
        public static IPlaneTimeInterval ProjectAt(IPlaneTimeInterval me, ITimePoint targetTime)
        {
            return new SimplePlaneTimeInterval(me.PlanePosition, me.ProjectAt(targetTime));
        }

        public static IPlaneTimeInterval ProjectAt(IPlaneTimeInterval me, IPlanePoint targetPosition)
        {
            throw new NotImplementedException();
        }
    }
}