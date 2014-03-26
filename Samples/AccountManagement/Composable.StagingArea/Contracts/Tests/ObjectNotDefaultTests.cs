using System;
using System.Collections.Generic;
using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Contracts.Tests
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

            Assert.Throws<ObjectIsDefaultContractViolationException>(() => Contract.Arguments(() => zero).NotDefault());
            Assert.Throws<ObjectIsDefaultContractViolationException>(() => Contract.Arguments(() => zero).NotDefault());
            Assert.Throws<ObjectIsDefaultContractViolationException>(() => Contract.Arguments(() => myStructure).NotDefault());
            Assert.Throws<ObjectIsDefaultContractViolationException>(() => Contract.Arguments(() => myStructure).NotDefault());

            var badValues = new List<object> {zero, myStructure};
            var goodValues = new List<object> {new Object(), "", Guid.NewGuid()};

            InspectionTestHelper.InspectBadValue<ObjectIsDefaultContractViolationException, MyStructure>(
                inspected => inspected.NotDefault(),
                new MyStructure());

            InspectionTestHelper.BatchTestInspection<ObjectIsDefaultContractViolationException, int>(
                inspected => inspected.NotDefault(),
                badValues: new List<int> {0},
                goodValues: new List<int> {1, 2, 3});
        }

        [Test]
        public void ShouldRun50TestsInOneMillisecond() //The Activator.CreateInstance stuff in the default check had me a bit worried. Seems I had no reason to be.
        {
            var one = 1;
            Contract.Arguments(() => one).NotDefault();//Warm things up.

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            for(int i = 0; i < 500; i++)
            {
                Contract.Arguments(() =>one).NotDefault();
            }
            stopWatch.Elapsed.Should().BeLessOrEqualTo(10.Milliseconds());
        }

        private struct MyStructure {}
    }
}
