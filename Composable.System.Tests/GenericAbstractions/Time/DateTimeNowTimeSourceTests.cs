using System;
using Composable.GenericAbstractions.Time;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.GenericAbstractions.Time
{
    [TestFixture]
    public class DateTimeNowTimeSourceTests
    {
        [Test]
        public void LocalNow_should_return_within_100_milliseconds_of_DateTimeNow()
        {
            ITimeSource uut = new DateTimeNowTimeSource();
            Math.Abs((uut.LocalNow - DateTime.Now).TotalMilliseconds).Should().BeLessThan(100);
        }

        [Test]
        public void UtcNow_should_return_within_100_milliseconds_of_DateTimeNow()
        {
            ITimeSource uut = new DateTimeNowTimeSource();
            Math.Abs((uut.UtcNow - DateTime.UtcNow).TotalMilliseconds).Should().BeLessThan(100);
        }
    }
}