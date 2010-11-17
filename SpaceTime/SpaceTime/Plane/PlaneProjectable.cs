using Composable.SpaceTime.Plane.Impl;
using Composable.SpaceTime.Plane;

namespace Composable.SpaceTime.Plane
{
    ///<summary>Methods on an <see cref="IPlaneProjectable{TProjection}"/></summary>
    public static class PlaneProjectable
    {
        /// <summary>Projects <paramref name="me"/> at the position derived by moving <paramref name="me"/> by <paramref name="movement"/> </summary>
        public static T Offset<T>(this T me, IPlaneMovement movement ) where T : IPlaneProjectable<T>
        {
            return me.ProjectAt(me.PlanePosition.AsPoint().OffsetBy(movement.AsPoint()).AsPlanePoint());
        }
    }
}