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

        public SimpleTimePoint(ITimePoint position)
        {
#pragma warning disable 612,618
            DateTimeValue = position.DateTimeValue;
#pragma warning restore 612,618
        }

        public ITimePoint TimePosition { get { return this; } }
    }
}