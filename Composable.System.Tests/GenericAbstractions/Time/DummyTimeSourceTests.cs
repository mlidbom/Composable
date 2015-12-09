using System;
using Composable.GenericAbstractions.Time;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.GenericAbstractions.Time
{
    [TestFixture]
    public class DummyTimeSourceTests
    {
        [Test]
        public void Converting_LocalNow_to_utc_now_returns_UtcNow()
        {
            ITimeSource uut = DummyTimeSource.Now;
            uut.LocalNow.ToUniversalTime().Should().Be(uut.UtcNow);
        }

        [Test]
        public void Now_should_return_within_100_milliseconds_of_DateTimeNow()
        {
            ITimeSource uut = DummyTimeSource.Now;
            Math.Abs((uut.LocalNow - DateTime.Now).TotalMilliseconds).Should().BeLessThan(100);
        }

        [Test]
        public void Now_should_return_within_100_millisecond_of_datetime_utcnow()
        {
            ITimeSource uut = DummyTimeSource.Now;
            Math.Abs((uut.UtcNow - DateTime.UtcNow).TotalMilliseconds).Should().BeLessThan(100);
        }

        [Test]
        public void Converting_UtcNow_to_Localtime_returns_LocalNow()
        {
            ITimeSource uut = DummyTimeSource.Now;
            uut.UtcNow.ToLocalTime().Should().Be(uut.LocalNow);
        }

        [Test]
        public void FromLocalTime_returns_an_instance_with_LocalTime_equal_to_supplied_value()
        {
            DateTime now = DateTime.Now;
            ITimeSource uut = DummyTimeSource.FromLocalTime(now);
            uut.LocalNow.Should().Be(now);
            uut.UtcNow.Should().Be(now.ToUniversalTime());
        }

        [Test]
        public void FromUtcTime_returns_an_instance_with_UtcTime_equal_to_supplied_value()
        {
            DateTime utcNow = DateTime.UtcNow;
            ITimeSource uut = DummyTimeSource.FromÚtcTime(utcNow);
            uut.UtcNow.Should().Be(utcNow);
        }

        [Test]
        public void Passing_a_parsed_date_to_FromUtcTime_results_in_UtcNow_being_that_date()
        {
            var dateTime = DateTime.Parse("2001-01-01 00:00");
            var source = DummyTimeSource.FromÚtcTime(dateTime);
            source.UtcNow.Should().Be(dateTime);
        }
    }
}