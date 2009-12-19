using Void.Plane;
using Void.PlaneTime.Impl;
using Void.Time;

namespace Void.PlaneTime
{
    public static class PlaneTimePoint
    {
        public static IPlaneTimePoint ProjectAt(this IPlaneTimePoint me, ITimePoint targetTime)
        {
            return new SimplePlaneTimePoint(me, TimePoint.ProjectAt(me, targetTime));
        }

        public static IPlaneTimePoint ProjectAt(this IPlaneTimePoint me, IPlanePoint targetPosition)
        {
            return new SimplePlaneTimePoint(PlanePoint.ProjectAt(me, targetPosition), me);
        }
    }
}