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
            InspectionTestHelper.BatchTestInspection<ObjectIsNullException, object>(
                inspected => inspected.NotNull(),
                badValues: new List<object> { null, null },
                goodValues: new List<object> { new object(), "", Guid.NewGuid() });
        }

        [Test]
        public void UsesArgumentNameForExceptionmessage()
        {
            string nullString = null;
            Assert.Throws<ObjectIsNullException>(() => Contract.Optimized.Argument(nullString, "nullString").NotNull())
                .Message.Should().Contain("nullString");

            Assert.Throws<ObjectIsNullException>(() => Contract.Arguments(() => nullString).NotNull())
                .Message.Should().Contain("nullString");
        }
    }
    // ReSharper restore ConvertToConstant.Local
    // ReSharper restore ExpressionIsAlwaysNull
}
