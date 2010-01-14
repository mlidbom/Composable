using System.Drawing;

namespace Void.Plane
{
    /// <summary>Provides extensions to <see cref="Point"/></summary>
    public static class PointEx
    {
        ///<summary>Returns a point that is the result of adding <paramref name="movement"/> interpreted as a vector to <paramref name="me"/></summary>
        public static Point OffsetBy(this Point me, Point movement)
        {
            me.Offset(movement);
            return me;
        }
    }
}