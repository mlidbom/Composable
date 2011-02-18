namespace Composable.SpaceTime.Time
{
    /// <summary>Contains extension methods for <see cref="ITimeProjectable{TProjection}"/></summary>
    public static class TimeProjectable
    {
        /// <summary>Returns an <typeparamref name="T"/> that resides at the position in time that is <paramref name="movement"/> distant from the position of <paramref name="me"/></summary>
        public static T Offset<T>(this T me, ITimeMovement movement) where T : ITimeProjectable<T>
        {
            return me.ProjectAt(TimePoint.FromDateTime(me.TimePosition.AsDateTime() + movement.AsTimeSpan()));
        }
    }
}