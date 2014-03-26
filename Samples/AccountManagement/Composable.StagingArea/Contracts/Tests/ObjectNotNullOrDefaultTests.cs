using System;
using System.Collections.Generic;
using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Contracts.Tests
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


            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Optimized.Argument(nullObject).NotNullOrDefault());
            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Optimized.Arguments(anObject, nullObject).NotNullOrDefault());
            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Optimized.Arguments(emptyString, nullObject, anObject).NotNullOrDefault());

            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Arguments(() => nullObject).NotNullOrDefault());
            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Arguments(() => anObject, () => nullObject).NotNullOrDefault());
            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Arguments(() => emptyString, () => nullObject, () => anObject).NotNullOrDefault());
        }

        [Test]
        public void ThrowsObjectIsDefaultExceptionIfAnyValueIsDefault()
        {
            var anObject = new object();
            string emptyString = "";
            var zero = 0;
            var defaultMyStructure = new MyStructure();
            var aMyStructure = new MyStructure(1);

            Assert.Throws<ObjectIsDefaultContractViolationException>(() => Contract.Optimized.Argument(zero).NotNullOrDefault());
            Assert.Throws<ObjectIsDefaultContractViolationException>(() => Contract.Optimized.Arguments(anObject, zero).NotNullOrDefault());
            Assert.Throws<ObjectIsDefaultContractViolationException>(() => Contract.Optimized.Arguments(emptyString, anObject, defaultMyStructure).NotNullOrDefault());
            Contract.Optimized.Arguments(emptyString, anObject, aMyStructure).NotNullOrDefault();

            Assert.Throws<ObjectIsDefaultContractViolationException>(() => Contract.Arguments(() => zero).NotNullOrDefault());
            Assert.Throws<ObjectIsDefaultContractViolationException>(() => Contract.Arguments(() => anObject, () => zero).NotNullOrDefault());
            Assert.Throws<ObjectIsDefaultContractViolationException>(() => Contract.Arguments(() => emptyString, () => anObject, () => defaultMyStructure).NotNullOrDefault());
            Contract.Arguments(() => emptyString, () => anObject, () => aMyStructure).NotNullOrDefault();


            InspectionTestHelper.BatchTestInspection<ObjectIsDefaultContractViolationException, object>(
                inspected => inspected.NotNullOrDefault(),
                badValues: new List<object> {zero, defaultMyStructure},
                goodValues: new List<object> {new object(), "", Guid.NewGuid()});
        }

        [Test]
        public void ShouldRun10TestsInOneMillisecond() //The Activator.CreateInstance stuff in the default check had me a bit worried. Seems I had no reason to be.
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            for(int i = 0; i < 100; i++)
            {
                Contract.Optimized.Argument(1).NotNullOrDefault();
            }
            stopWatch.Elapsed.Should().BeLessOrEqualTo(10.Milliseconds());
        }

        private struct MyStructure
        {
            // ReSharper disable NotAccessedField.Local
            private int _value;
            // ReSharper restore NotAccessedField.Local

            public MyStructure(int value)
            {
                _value = value;
            }
        }
    }

    // ReSharper restore ConvertToConstant.Local
    // ReSharper restore ExpressionIsAlwaysNull
}
