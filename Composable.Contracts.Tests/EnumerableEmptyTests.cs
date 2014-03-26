using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    [TestFixture]
    public class EnumerableEmptyTests
    {
        [Test]
        public void ThrowsEnumerableIsEmptyException()
        {
            var emptyStringList = new List<string>();
            Assert.Throws<EnumerableIsEmptyContractViolationException>(() => Contract.Arguments(() => emptyStringList).NotNullOrEmptyEnumerable());

            var exception = Assert.Throws<EnumerableIsEmptyContractViolationException>(() => Contract.Arguments(() => emptyStringList).NotNullOrEmptyEnumerable());
            exception.BadValue.Type.Should().Be(InspectionType.Argument);

            exception = Assert.Throws<EnumerableIsEmptyContractViolationException>(() => Contract.Invariant(() => emptyStringList).NotNullOrEmptyEnumerable());
            exception.BadValue.Type.Should().Be(InspectionType.Invariant);

            exception = Assert.Throws<EnumerableIsEmptyContractViolationException>(() => ReturnValueContractHelper.Return(emptyStringList, inspected => inspected.NotNullOrEmptyEnumerable()));
            exception.BadValue.Type.Should().Be(InspectionType.ReturnValue);


            InspectionTestHelper.BatchTestInspection<EnumerableIsEmptyContractViolationException, IEnumerable<string>>(
                assert: inspected => inspected.NotNullOrEmptyEnumerable(),
                badValues: new List<IEnumerable<string>> {emptyStringList, new List<string>()},
                goodValues: new List<IEnumerable<string>> {new List<string> {""}, new List<string> {""}});
        }
    }
}
