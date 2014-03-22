using System;
using Composable.Contracts;
using NUnit.Framework;

namespace Composable.StagingArea
{
    [TestFixture]
    public class ContractTests
    {
        [Test]
        public void ArgumentNotNullThrowsArgumentNullExceptionIfAnyArgumentIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => Contract.ArgumentNotNull(null));
            Assert.Throws<ArgumentNullException>(() => Contract.ArgumentNotNull("", null));
            Assert.Throws<ArgumentNullException>(() => Contract.ArgumentNotNull("", null, ""));
        }

        [Test]
        public void NotEmptyThrowsArgumentExceptionForEmptyGuid()
        {
            Assert.Throws<ArgumentException>(() => Contract.Argument(Guid.Empty).NotEmpty());
        }
    }
}