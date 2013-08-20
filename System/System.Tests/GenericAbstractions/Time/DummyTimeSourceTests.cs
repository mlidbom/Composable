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
            ITimeSource uut = DummyTimeTimeSource.Now;
            uut.LocalNow.ToUniversalTime().Should().Be(uut.UtcNow);
        }

        [Test]
        public void Now_should_return_within_100_milliseconds_of_DateTimeNow()
        {
            ITimeSource uut = DummyTimeTimeSource.Now;
            Math.Abs((uut.LocalNow - DateTime.Now).TotalMilliseconds).Should().BeLessThan(100);
        }

        [Test]
        public void Now_should_return_within_100_millisecond_of_datetime_utcnow()
        {
            ITimeSource uut = DummyTimeTimeSource.Now;
            Math.Abs((uut.UtcNow - DateTime.UtcNow).TotalMilliseconds).Should().BeLessThan(100);
        }

        [Test]
        public void Converting_UtcNow_to_Localtime_returns_LocalNow()
        {
            ITimeSource uut = DummyTimeTimeSource.Now;
            uut.UtcNow.ToLocalTime().Should().Be(uut.LocalNow);
        }

        [Test]
        public void FromLocalTime_returns_an_instance_with_LocalTime_equal_to_supplied_value()
        {
            DateTime now = DateTime.Now;
            ITimeSource uut = DummyTimeTimeSource.FromLocalTime(now);
            uut.LocalNow.Should().Be(now);
        }

        [Test]
        public void FromUtcTime_returns_an_instance_with_UtcTime_equal_to_supplied_value()
        {
            DateTime utcNow = DateTime.UtcNow;
            ITimeSource uut = DummyTimeTimeSource.FromÚtcTime(utcNow);
            uut.UtcNow.Should().Be(utcNow);
        }
    }
}