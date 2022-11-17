using System;
using System.Globalization;
using Composable.SystemCE;

namespace Composable.GenericAbstractions.Time
{
    /// <summary> Just statically returns whatever value was assigned.</summary>
    class TestingTimeSource : IUtcTimeTimeSource
    {
        DateTime? _freezeAt;


        ///<summary>Returns a timesource that will continually return the time that it was created at as the current time.</summary>
        internal static TestingTimeSource FollowingSystemClock => new TestingTimeSource();

        ///<summary>Returns a timesource that will continually return the time that it was created at as the current time.</summary>
        internal static TestingTimeSource FrozenUtcNow() => new TestingTimeSource
                                                         {
                                                             _freezeAt = DateTime.UtcNow
                                                         };

        ///<summary>Returns a timesource that will forever return <param name="utcTime"> as the current time.</param></summary>
        internal static TestingTimeSource FrozenAtUtcTime(DateTime utcTime) => new TestingTimeSource
                                                                                 {
                                                                                     _freezeAt = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc)
                                                                                 };

        public void FreezeAtUtcTime(DateTime time) => _freezeAt = time.ToUniversalTimeSafely();

        public void FreezeAtUtcTime(String time) => FreezeAtUtcTime(DateTime.Parse(time, CultureInfo.InvariantCulture).ToUniversalTime());

        ///<summary>Gets or sets the current UTC time.</summary>
        public DateTime UtcNow
        {
            get
            {
                if(_freezeAt != null) return _freezeAt.Value;
                return DateTime.UtcNow;
            }
        }
    }
}