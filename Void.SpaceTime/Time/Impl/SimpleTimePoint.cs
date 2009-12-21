using System;

namespace Void.Time.Impl
{
    [Obsolete(WarningMessages.InternalAndInheritanceOnly)]
    public class SimpleTimePoint : ITimePoint
    {
        public DateTime DateTimeValue { get; private set; }
        protected internal SimpleTimePoint(DateTime position)
        {
            DateTimeValue = position;
        }

        public ITimePoint TimePosition { get { return this; } }
    }
}