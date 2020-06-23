﻿using System;
using Composable.GenericAbstractions.Time;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.GenericAbstractions.Time
{
    [TestFixture]
    public class DummyTimeSourceTests
    {
        [Test]
        public void Now_should_return_within_100_millisecond_of_datetime_UtcNow()
        {
            var uut = TestingTimeSource.FrozenUtcNow();
            Math.Abs((uut.UtcNow - DateTime.UtcNow).TotalMilliseconds).Should().BeLessThan(100);
        }

        [Test]
        public void FromUtcTime_returns_an_instance_with_UtcTime_equal_to_supplied_value()
        {
            var utcNow = DateTime.UtcNow;
            var uut = TestingTimeSource.FrozenAtUtcTime(utcNow);
            uut.UtcNow.Should().Be(utcNow);
        }

        [Test]
        public void Passing_a_parsed_date_to_FromUtcTime_results_in_UtcNow_being_that_date()
        {
            var dateTime = DateTime.Parse("2001-01-01 00:00");
            var source = TestingTimeSource.FrozenAtUtcTime(dateTime);
            source.UtcNow.Should().Be(dateTime);
        }
    }
}