using System;
using System.Collections.Generic;
using Composable.Contracts;
using FluentAssertions;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Composable.Tests.Contracts
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

            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Argument(() => nullString).NotNull());
            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Argument(() => anObject, () => nullString).NotNull());
            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Argument(() => nullString).NotNull())
                .Message.Should().Contain("nullString");

            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Invariant(nullString, nameof(nullString)).NotNull());
            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Invariant(anObject, nameof(anObject), nullString, nameof(nullString)).NotNull());
            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Invariant(nullString, nameof(nullString)).NotNull())
                .Message.Should().Contain("nullString");
        }

        [Test]
        public void UsesArgumentNameForExceptionmessage()
        {
            string nullString = null;

            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Argument(() => nullString).NotNull())
                .Message.Should().Contain("nullString");
        }
    }

    // ReSharper restore ConvertToConstant.Local
    // ReSharper restore ExpressionIsAlwaysNull
}
