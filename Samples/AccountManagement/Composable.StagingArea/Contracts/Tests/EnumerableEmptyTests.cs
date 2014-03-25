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
            Assert.Throws<EnumerableIsEmptyException>(() => Contract.Optimized.Argument(emptyStringList).NotNullOrEmptyEnumerable());

            var exception = Assert.Throws<EnumerableIsEmptyException>(() => Contract.Arguments(() => emptyStringList).NotNullOrEmptyEnumerable());
            exception.BadValue.Type.Should().Be(InspectionType.Argument);

            exception = Assert.Throws<EnumerableIsEmptyException>(() => Contract.Invariant(() => emptyStringList).NotNullOrEmptyEnumerable());
            exception.BadValue.Type.Should().Be(InspectionType.Invariant);

            exception = Assert.Throws<EnumerableIsEmptyException>(() => ReturnValueContractHelper.Return(emptyStringList, inspected => inspected.NotNullOrEmptyEnumerable()));
            exception.BadValue.Type.Should().Be(InspectionType.ReturnValue);


            InspectionTestHelper.BatchTestInspection<EnumerableIsEmptyException, IEnumerable<string>>(
                assert: inspected => inspected.NotNullOrEmptyEnumerable(),
                badValues: new List<IEnumerable<string>> {emptyStringList, new List<string>()},
                goodValues: new List<IEnumerable<string>> {new List<string> {""}, new List<string> {""}});
        }
    }
}
