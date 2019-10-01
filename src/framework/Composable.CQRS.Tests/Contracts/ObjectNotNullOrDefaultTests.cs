using System;
using System.Collections.Generic;
using Composable.Contracts;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Composable.Tests.Contracts
{
    // ReSharper disable ConvertToConstant.Local
    // ReSharper disable ExpressionIsAlwaysNull
    [TestFixture]
    public class ObjectNotNullOrDefaultTests
    {
        [Test]
        public void ThrowsArgumentNullExceptionIfAnyValueIsNull()
        {
            var anObject = new object();
            object nullObject = null;
            var emptyString = "";

            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Argument(() => nullObject).NotNullOrDefault());
            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Argument(() => anObject, () => nullObject).NotNullOrDefault());
            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Argument(() => emptyString, () => nullObject, () => anObject).NotNullOrDefault());
        }

        [Test]
        public void ThrowsObjectIsDefaultExceptionIfAnyValueIsDefault()
        {
            var anObject = new object();
            var emptyString = "";
            var zero = 0;
            var defaultMyStructure = new MyStructure();
            var aMyStructure = new MyStructure(1);

            Assert.Throws<ObjectIsDefaultContractViolationException>(() => Contract.Argument(() => zero).NotNullOrDefault());
            Assert.Throws<ObjectIsDefaultContractViolationException>(() => Contract.Argument(() => anObject, () => zero).NotNullOrDefault());
            Assert.Throws<ObjectIsDefaultContractViolationException>(() => Contract.Argument(() => emptyString, () => anObject, () => defaultMyStructure).NotNullOrDefault());
            Contract.Argument(() => emptyString, () => anObject, () => aMyStructure).NotNullOrDefault();


            InspectionTestHelper.BatchTestInspection<ObjectIsDefaultContractViolationException, object>(
                inspected => inspected.NotNullOrDefault(),
                badValues: new List<object> {zero, defaultMyStructure},
                goodValues: new List<object> {new object(), "", Guid.NewGuid()});
        }

        struct MyStructure
        {
            // ReSharper disable NotAccessedField.Local
#pragma warning disable IDE0052 // Remove unread private members
            int _value;
#pragma warning restore IDE0052 // Remove unread private members
            // ReSharper restore NotAccessedField.Local

            public MyStructure(int value) => _value = value;
        }
    }

    // ReSharper restore ConvertToConstant.Local
    // ReSharper restore ExpressionIsAlwaysNull
}
