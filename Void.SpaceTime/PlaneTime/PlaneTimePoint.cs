using Void.Plane;
using Void.PlaneTime.Impl;
using Void.Time;
using Void.Tobii.Impl;

namespace Void.PlaneTime
{
    public static class PlaneTimePoint
    {
        public static IPlaneTimePoint MoveTo(this IPlaneTimePoint me, ITimePoint targetTime)
        {
            return new SimplePlaneTimePoint(me, TimePoint.MoveTo(me, targetTime));
        }

        public static IPlaneTimePoint MoveTo(this IPlaneTimePoint me, IPlanePoint targetPosition)
        {
            return new SimplePlaneTimePoint(PlanePoint.MoveTo(me, targetPosition), me);
        }
    }
}