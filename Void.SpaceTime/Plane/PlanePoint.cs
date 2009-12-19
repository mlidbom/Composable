using Void.Plane.Impl;

namespace Void.Plane
{
    public static class PlanePoint
    {
        public static IPlanePoint Offset(this IPlanePoint me, IPlaneMovement movement)
        {
            return new SimplePlanePoint(me.XCoordinate + movement.XMovement, me.YCoordinate + movement.YMovement);
        }
    }
}