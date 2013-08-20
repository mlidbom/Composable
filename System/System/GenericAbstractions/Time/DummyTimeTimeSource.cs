using System;

namespace Composable.GenericAbstractions.Time
{
    /// <summary> Just statically returns whatever value was assigned.</summary>
    public class DummyTimeTimeSource : ITimeSource
    {
        private DummyTimeTimeSource(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public static DummyTimeTimeSource Now { get{return new DummyTimeTimeSource(DateTime.UtcNow);}}
        public static DummyTimeTimeSource FromLocalTime(DateTime localTime) { return new DummyTimeTimeSource(localTime.ToUniversalTime());  }
        public static DummyTimeTimeSource FromÚtcTime(DateTime utcTime) { return new DummyTimeTimeSource(utcTime); }

        public DateTime UtcNow { get; private set; }
        public DateTime LocalNow {get { return UtcNow.ToLocalTime(); }}
    }
}