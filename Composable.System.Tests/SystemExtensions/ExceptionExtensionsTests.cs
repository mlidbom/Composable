using System;
using System.Collections.Generic;
using Composable.System.Linq;
using NUnit.Framework;
using Composable.SystemExtensions;
using FluentAssertions;

namespace Composable.Tests.SystemExtensions
{
    [TestFixture]
    public class ExceptionExtensions
    {
        private Exception _originalException;
        private Exception _firstNestingException;
        private Exception _secondNestingException;

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
            var expected = Seq.Create(_secondNestingException, _firstNestingException, _originalException);

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