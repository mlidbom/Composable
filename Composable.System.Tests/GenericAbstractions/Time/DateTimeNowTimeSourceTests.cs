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
            uut.LocalNow.Should().BeWithin(100.Milliseconds()).Before(DateTime.Now);
        }

        [Test]
        public void UtcNow_should_return_within_100_milliseconds_of_DateTimeNow()
        {
            ITimeSource uut = new DateTimeNowTimeSource();
            uut.UtcNow.Should().BeWithin(100.Milliseconds()).Before(DateTime.UtcNow);
        }
    }
}