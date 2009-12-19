using Void.Time.Impl;

namespace Void.Time
{
    public static class TimePoint
    {
        public static ITimePoint ProjectAt(this ITimePoint me, ITimePoint targetTime)
        {
            return new SimpleTimePoint(targetTime);
        }
    }
}