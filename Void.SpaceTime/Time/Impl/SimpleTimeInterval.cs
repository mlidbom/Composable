using System;

namespace Void.Time
{
    public class SimpleTimeInterval : ITimeInterval
    {
        public ITimePoint TimePosition { get; private set; }
        public TimeSpan Duration { get; private set; }

        public SimpleTimeInterval(ITimeInterval template) : this(template.TimePosition, template.Duration)
        {
        }

        public SimpleTimeInterval(ITimePoint timeCoordinate, TimeSpan duration)
        {
            TimePosition = timeCoordinate;
            Duration = duration;
        }
    }
}