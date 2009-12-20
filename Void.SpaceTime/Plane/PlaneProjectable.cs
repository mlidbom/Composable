using Void.Plane.Impl;

namespace Void.Plane
{
    public static class PlaneProjectable
    {
        public static T Offset<T>(this T me, IPlaneMovement movement ) where T : IPlaneProjectable<T>
        {
            return me.At(me.X() + movement.X(), me.Y() + movement.Y());
        }

        #region enable non-warning access to internal use only members
        #pragma warning disable 618

        private static int Y(this IPlanePositioned me)
        {
            return me.PlanePosition.YCoordinate;
        }

        private static int X(this IPlanePositioned me)
        {
            return me.PlanePosition.XCoordinate;
        }

        private static int Y(this IPlaneMovement movement)
        {
            return movement.YMovement;
        }

        private static int X(this IPlaneMovement movement)
        {
            return movement.XMovement;
        }

        private static T At<T>(this T me, int xCoordinate, int yCoordinate) where T : IPlaneProjectable<T>
        {
            return me.ProjectAt(new SimplePlanePoint(xCoordinate, yCoordinate));
        }
        #pragma warning restore 618
        #endregion
    }
}