using System;
using Composable.System.Reactive;

namespace Composable.GenericAbstractions.Time
{
    /// <summary> Just statically returns whatever value was assigned.</summary>
    public class DummyTimeSource : IUtcTimeTimeSource
    {
        private DateTime _utcNow;

        private DummyTimeSource(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        ///<summary>Returns a timesource that will continually return the time that it was created at as the current time.</summary>
        public static DummyTimeSource Now { get{return new DummyTimeSource(DateTime.UtcNow);}}

        ///<summary>Returns a timesource that will forever return <param name="localTime"> as the current time.</param></summary>
        public static DummyTimeSource FromLocalTime(DateTime localTime) { return new DummyTimeSource(DateTime.SpecifyKind(localTime, DateTimeKind.Local).ToUniversalTime());  }
        ///<summary>Returns a timesource that will forever return <param name="utcTime"> as the current time.</param></summary>
        public static DummyTimeSource FromÚtcTime(DateTime utcTime) { return new DummyTimeSource(DateTime.SpecifyKind(utcTime, DateTimeKind.Utc)); }


        ///<summary>Allows for subscribing to notifications about <see cref="UtcNow"/> changing.</summary>
        public IObservable<DateTime> UtcNowChanged { get { return _utcNowChanged; } }

        private readonly SimpleObservable<DateTime> _utcNowChanged = new SimpleObservable<DateTime>();

        private void NotifyListeners()
        {
            _utcNowChanged.OnNext(UtcNow);
        }

        ///<summary>Gets or sets the current UTC time.</summary>
        public DateTime UtcNow
        {
            get
            {
                return _utcNow;
            }
            set
            {
                _utcNow = DateTime.SpecifyKind(value, DateTimeKind.Utc);
                NotifyListeners();
            }
        }
    }
}