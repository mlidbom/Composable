using System;

namespace Void.Time.Impl
{
    public class SimpleTimePoint : ITimePoint
    {
        public DateTime DateTimeValue { get; private set; }
        protected internal SimpleTimePoint(DateTime position)
        {
            DateTimeValue = position;
        }

        public SimpleTimePoint(ITimePoint position)
        {
            DateTimeValue = position.DateTimeValue;
        }        
    }
}