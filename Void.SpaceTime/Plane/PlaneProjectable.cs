namespace Void.Plane
{
    public static class PlaneProjectable
    {
        public static T Offset<T>(this T me, IPlaneMovement movement ) where T : IPlaneProjectable<T>
        {
            return me.ProjectAt(me.PlanePosition.Offset(movement));   
        }
    }
}