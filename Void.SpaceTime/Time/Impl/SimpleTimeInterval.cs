using System;

namespace Void.Time
{
    public class SimpleTimeInterval : ITimeInterval
    {
        public ITimePoint TimeCoordinate { get; private set; }
        public TimeSpan Duration { get; private set; }

        public SimpleTimeInterval(ITimeInterval template) : this(template.TimeCoordinate, template.Duration)
        {
        }

        public SimpleTimeInterval(ITimePoint timeCoordinate, TimeSpan duration)
        {
            TimeCoordinate = timeCoordinate;
            Duration = duration;
        }
    }
}