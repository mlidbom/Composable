using System;

namespace Void.Time.Impl
{
    [Obsolete(WarningMessages.InternalAndInheritanceOnly)]
    public class SimpleTimeInterval : ITimeInterval
    {
        public ITimePoint TimePosition { get; private set; }
        public IDuration Duration { get; private set; }

        public SimpleTimeInterval(ITimeInterval template) : this(template.TimePosition, template.Duration)
        {
        }

        public SimpleTimeInterval(ITimePoint timeCoordinate, IDuration duration)
        {
            TimePosition = timeCoordinate;
            Duration = duration;
        }
    }
}