using System;

namespace Void.Time.Impl
{
    public class SimpleTimePoint : ITimePoint
    {
        private readonly DateTime _datetimeValue;

        public SimpleTimePoint(DateTime position)
        {
            _datetimeValue = position;
        }

        public SimpleTimePoint(ITimePoint position)
        {
            _datetimeValue = position.DateTimeValue;
        }

        public DateTime DateTimeValue
        {
            get { return _datetimeValue; }
        }

        public ITimePoint TimeCoordinate
        {
            get { return this; }
        }
    }
}