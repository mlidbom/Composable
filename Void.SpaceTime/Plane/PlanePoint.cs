using Void.Plane.Impl;

namespace Void.Plane
{
    public static class PlanePoint
    {
        public static IPlanePoint Offset(this IPlanePoint me, IPlaneMovement movement)
        {
            return At(me.X() + movement.X(), me.Y() + movement.Y());
        }

        #region enable non-warning access to internal use only members 
        #pragma warning disable 618

        private static int Y(this IPlanePoint me)
        {

            return me.YCoordinate;
        }

        private static int X(this IPlanePoint me)
        {
            return me.XCoordinate;
        }

        private static int Y(this IPlaneMovement movement)
        {
            return movement.YMovement;
        }

        private static int X(this IPlaneMovement movement)
        {
            return movement.XMovement;
        }

        private static IPlanePoint At(int xCoordinate, int yCoordinate)
        {
            return new SimplePlanePoint(xCoordinate, yCoordinate);
        }
        #pragma warning restore 618
        #endregion
    }
}