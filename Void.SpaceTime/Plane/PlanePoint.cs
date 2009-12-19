using Void.Plane.Impl;

namespace Void.Plane
{
    public static class PlanePoint
    {
        public static IPlanePoint MoveTo(this IPlanePoint me, IPlanePoint targetPosition)
        {
            return new SimplePlanePoint(me.XCoordinate, me.YCoordinate);
        }
    }
}