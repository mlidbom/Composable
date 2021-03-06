﻿using System;
using Composable.SystemCE;
using Composable.SystemCE.LinqCE;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.SystemExtensions
{
    [TestFixture]
    public class ExceptionExtensionsTests
    {
        Exception _originalException;
        Exception _firstNestingException;
        Exception _secondNestingException;

        [SetUp]
        public void Setup()
        {
            _originalException = new Exception("Root cause exception");
            _firstNestingException = new Exception("nested once", _originalException);
            _secondNestingException = new Exception("nested twice", _firstNestingException);
        }

        [Test]
        public void GetAllExceptionsInStackShouldReturnAllNestedExceptionsInOrderFromRootToMostNestedException()
        {
            var expected = EnumerableCE.Create(_secondNestingException, _firstNestingException, _originalException);

            var actual = _secondNestingException.GetAllExceptionsInStack();

            actual.Should().Equal(expected);

        }

        [Test]
        public void GetRootCauseExceptionShouldReturnMostNestedException()
        {
            _secondNestingException.GetRootCauseException().Should().Be(_originalException);
        }
    }
}