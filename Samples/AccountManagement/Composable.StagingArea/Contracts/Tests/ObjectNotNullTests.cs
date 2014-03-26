using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    // ReSharper disable ConvertToConstant.Local
    // ReSharper disable ExpressionIsAlwaysNull
    [TestFixture]
    public class ObjectNotNullTests
    {
        [Test]
        public void ThrowsObjectNullExceptionForNullValues()
        {
            InspectionTestHelper.BatchTestInspection<ObjectIsNullContractViolationException, object>(
                inspected => inspected.NotNull(),
                badValues: new List<object> {null, null},
                goodValues: new List<object> {new object(), "", Guid.NewGuid()});


            var nullString = (string)null;
            var anObject = new object();

            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Optimized.Arguments(nullString).NotNull());
            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Optimized.Arguments(anObject, nullString).NotNull());
            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Optimized.Argument(nullString, "nullString").NotNull())
                .Message.Should().Contain("nullString");

            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Optimized.Invariant(nullString).NotNull());
            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Optimized.Invariant(anObject, nullString).NotNull());
            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Optimized.NamedInvariant(nullString, "nullString").NotNull())
                .Message.Should().Contain("nullString");
        }

        [Test]
        public void UsesArgumentNameForExceptionmessage()
        {
            string nullString = null;
            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Optimized.Argument(nullString, "nullString").NotNull())
                .Message.Should().Contain("nullString");

            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Arguments(() => nullString).NotNull())
                .Message.Should().Contain("nullString");
        }
    }

    // ReSharper restore ConvertToConstant.Local
    // ReSharper restore ExpressionIsAlwaysNull
}
