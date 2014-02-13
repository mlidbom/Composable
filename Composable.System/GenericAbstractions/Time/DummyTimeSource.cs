using System;

namespace Composable.GenericAbstractions.Time
{
    /// <summary> Just statically returns whatever value was assigned.</summary>
    public class DummyTimeSource : ITimeSource
    {
        private DateTime _utcNow;
        private DateTime _localNow;

        private DummyTimeSource(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public static DummyTimeSource Now { get{return new DummyTimeSource(DateTime.UtcNow);}}
        public static DummyTimeSource FromLocalTime(DateTime localTime) { return new DummyTimeSource(localTime.ToUniversalTime());  }
        public static DummyTimeSource FromÚtcTime(DateTime utcTime) { return new DummyTimeSource(utcTime); }

        public DateTime UtcNow
        {
            get
            {
                return _utcNow;
            }
            set
            {
                _utcNow = value.ToUniversalTime();
                _localNow = _utcNow.ToLocalTime();
            }
        }

        public DateTime LocalNow
        {
            get
            {
                return _localNow;
            }
            set
            {
                _localNow = value.ToLocalTime();
                _utcNow = _localNow.ToUniversalTime();
            }
        }
    }
}