using System;
using System.Collections.Generic;
using Composable.Contracts;
using NUnit.Framework;

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
            string emptyString = "";

            Assert.Throws<ObjectIsNullContractViolationException>(() => OldContract.Argument(() => nullObject).NotNullOrDefault());
            Assert.Throws<ObjectIsNullContractViolationException>(() => OldContract.Argument(() => anObject, () => nullObject).NotNullOrDefault());
            Assert.Throws<ObjectIsNullContractViolationException>(() => OldContract.Argument(() => emptyString, () => nullObject, () => anObject).NotNullOrDefault());
        }

        [Test]
        public void ThrowsObjectIsDefaultExceptionIfAnyValueIsDefault()
        {
            var anObject = new object();
            string emptyString = "";
            var zero = 0;
            var defaultMyStructure = new MyStructure();
            var aMyStructure = new MyStructure(1);

            Assert.Throws<ObjectIsDefaultContractViolationException>(() => OldContract.Argument(() => zero).NotNullOrDefault());
            Assert.Throws<ObjectIsDefaultContractViolationException>(() => OldContract.Argument(() => anObject, () => zero).NotNullOrDefault());
            Assert.Throws<ObjectIsDefaultContractViolationException>(() => OldContract.Argument(() => emptyString, () => anObject, () => defaultMyStructure).NotNullOrDefault());
            OldContract.Argument(() => emptyString, () => anObject, () => aMyStructure).NotNullOrDefault();


            InspectionTestHelper.BatchTestInspection<ObjectIsDefaultContractViolationException, object>(
                inspected => inspected.NotNullOrDefault(),
                badValues: new List<object> {zero, defaultMyStructure},
                goodValues: new List<object> {new object(), "", Guid.NewGuid()});
        }

        struct MyStructure
        {
            // ReSharper disable NotAccessedField.Local
            int _value;
            // ReSharper restore NotAccessedField.Local

            public MyStructure(int value) => _value = value;
        }
    }

    // ReSharper restore ConvertToConstant.Local
    // ReSharper restore ExpressionIsAlwaysNull
}
