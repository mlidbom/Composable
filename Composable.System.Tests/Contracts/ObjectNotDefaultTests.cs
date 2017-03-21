using System.Collections.Generic;
using Composable.Contracts;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.Contracts
{
    [TestFixture]
    public class ObjectNotDefaultTests
    {
        [Test]
        public void ThrowsObjectIsDefaultExceptionIfAnyValueIsDefault()
        {
            var myStructure = new MyStructure();
            // ReSharper disable ConvertToConstant.Local
            var zero = 0;
            // ReSharper restore ConvertToConstant.Local

            Assert.Throws<ObjectIsDefaultContractViolationException>(() => Contract.Argument(() => zero).NotDefault());
            Assert.Throws<ObjectIsDefaultContractViolationException>(() => Contract.Argument(() => zero).NotDefault());
            Assert.Throws<ObjectIsDefaultContractViolationException>(() => Contract.Argument(() => myStructure).NotDefault());
            Assert.Throws<ObjectIsDefaultContractViolationException>(() => Contract.Argument(() => myStructure).NotDefault());

            InspectionTestHelper.InspectBadValue<ObjectIsDefaultContractViolationException, MyStructure>(
                inspected => inspected.NotDefault(),
                new MyStructure());

            InspectionTestHelper.BatchTestInspection<ObjectIsDefaultContractViolationException, int>(
                inspected => inspected.NotDefault(),
                badValues: new List<int> {0},
                goodValues: new List<int> {1, 2, 3});
        }

        [Test, Category(TestCategories.PerformanceTest)]
        public void ShouldRun500TestsIn10Milliseconds() //The Activator.CreateInstance stuff in the default check had me a bit worried. Seems I had no reason to be.
        {
            var one = 1;

            TimeAsserter.Execute(
                action: () => Contract.Argument(() => one).NotDefault(),
                iterations: 500,
                maxTotal: 10.Milliseconds().AdjustRuntimeToTestEnvironment());
        }

        struct MyStructure {}
    }
}
