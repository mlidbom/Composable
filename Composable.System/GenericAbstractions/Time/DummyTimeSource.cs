using System;
using Composable.System.Reactive;

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

        ///<summary>Returns a timesource that will continually return the time that it was created at as the current time.</summary>
        public static DummyTimeSource Now { get{return new DummyTimeSource(DateTime.UtcNow);}}

        ///<summary>Returns a timesource that will forever return <param name="localTime"> as the current time.</param></summary>
        public static DummyTimeSource FromLocalTime(DateTime localTime) { return new DummyTimeSource(localTime.ToUniversalTime());  }
        ///<summary>Returns a timesource that will forever return <param name="utcTime"> as the current time.</param></summary>
        public static DummyTimeSource FromÚtcTime(DateTime utcTime) { return new DummyTimeSource(utcTime); }


        ///<summary>Allows for subscribing to notifications about <see cref="UtcNow"/> changing.</summary>
        public IObservable<DateTime> UtcChanged { get { return _utcChanged; } }
        ///<summary>Allows for subscribing to notifications about <see cref="LocalNow"/> changing.</summary>
        public IObservable<DateTime> LocalChanged { get { return _localChanged; } }

        private readonly SimpleObservable<DateTime> _utcChanged = new SimpleObservable<DateTime>();
        private readonly SimpleObservable<DateTime> _localChanged = new SimpleObservable<DateTime>();

        private void NotifyListeners()
        {
            _utcChanged.OnNext(UtcNow);
            _localChanged.OnNext(LocalNow);
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
                _utcNow = value.ToUniversalTime();
                _localNow = _utcNow.ToLocalTime();
                NotifyListeners();
            }
        }

        ///<summary>Gets or sets the current local time.</summary>
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
                NotifyListeners();
            }
        }
    }
}