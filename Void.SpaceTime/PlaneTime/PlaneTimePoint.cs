using Void.Plane;
using Void.PlaneTime.Impl;
using Void.Time;

namespace Void.PlaneTime
{
    public static class PlaneTimePoint
    {
        public static IPlaneTimePoint Offset(this IPlaneTimePoint me, ITimeMovement movement)
        {
            return new SimplePlaneTimePoint(me, TimePoint.Offset(me, movement));
        }

        public static IPlaneTimePoint Offset(this IPlaneTimePoint me, IPlaneMovement movement)
        {
            return new SimplePlaneTimePoint(PlanePoint.Offset(me, movement), me);
        }
    }
}