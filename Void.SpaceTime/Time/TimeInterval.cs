namespace Void.Time
{
    public static class TimeInterval
    {
        public static ITimeInterval MoveTo(this ITimeInterval me, ITimePoint targetTime)
        {
            return new SimpleTimeInterval(targetTime.TimeCoordinate, me.Duration);
        }
    }
}