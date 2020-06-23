using System.Collections.Generic;
using Composable.Contracts;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Composable.Tests.Contracts
{
    [TestFixture]
    public class ObjectNotDefaultTests
    {
        [Test]
        public void ThrowsObjectIsDefaultExceptionIfAnyValueIsDefault()
        {
            var myDefaultStructure = new MyStructure();
            // ReSharper disable ConvertToConstant.Local
            var zero = 0;
            // ReSharper restore ConvertToConstant.Local

            Assert.Throws<ObjectIsDefaultContractViolationException>(() => Contract.Argument(() => zero).NotDefault());
            Assert.Throws<ObjectIsDefaultContractViolationException>(() => Contract.Argument(() => zero).NotDefault());
            Assert.Throws<ObjectIsDefaultContractViolationException>(() => Contract.Argument(() => myDefaultStructure).NotDefault());
            Assert.Throws<ObjectIsDefaultContractViolationException>(() => Contract.Argument(() => myDefaultStructure).NotDefault());

            var myNonDefaultStructure = new MyStructure
                                        {
                                Value = 2
                            };

            Contract.Argument(() => myNonDefaultStructure).NotDefault();

            InspectionTestHelper.InspectBadValue<ObjectIsDefaultContractViolationException, MyStructure>(
                inspected => inspected.NotDefault(),
                new MyStructure());

            InspectionTestHelper.BatchTestInspection<ObjectIsDefaultContractViolationException, int>(
                inspected => inspected.NotDefault(),
                badValues: new List<int> {0},
                goodValues: new List<int> {1, 2, 3});
        }

        struct MyStructure
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public int Value { get; set; }
        }
    }
}
