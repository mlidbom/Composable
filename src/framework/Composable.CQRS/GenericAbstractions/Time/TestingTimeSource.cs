using System;

namespace Composable.GenericAbstractions.Time
{
    /// <summary> Just statically returns whatever value was assigned.</summary>
    class TestingTimeSource : IUtcTimeTimeSource
    {
        TimeSpan? _offset = null;
        DateTime? _freezeAt;


        ///<summary>Returns a timesource that will continually return the time that it was created at as the current time.</summary>
        internal static TestingTimeSource FollowingSystemClock => new TestingTimeSource();

        ///<summary>Returns a timesource that will continually return the time that it was created at as the current time.</summary>
        internal static TestingTimeSource FrozenNow => new TestingTimeSource
                                                 {
                                                     _freezeAt = DateTime.UtcNow
                                                 };


        ///<summary>Returns a timesource that will forever return <param name="utcTime"> as the current time.</param></summary>
        internal static TestingTimeSource FrozenFromUtcTime(DateTime utcTime) => new TestingTimeSource
                                                                                 {
                                                                                     _freezeAt = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc)
                                                                                 };

        public void FreezeAt(DateTime time) => _freezeAt = time;

        ///<summary>Gets or sets the current UTC time.</summary>
        public DateTime UtcNow
        {
            get
            {
                if(_freezeAt != null) return _freezeAt.Value;
                if(_offset != null) return DateTime.UtcNow + _offset.Value;

                return DateTime.UtcNow;
            }
        }
    }
}