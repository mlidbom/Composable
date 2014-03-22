using System;
using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    [TestFixture]
    public class ArgumentNotNullTests
    {
        [Test]
        public void ThrowsArgumentNullExceptionIfAnyArgumentIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => Contract.ArgumentNotNull(null));
            Assert.Throws<ArgumentNullException>(() => Contract.ArgumentNotNull(new object(), null));
            Assert.Throws<ArgumentNullException>(() => Contract.ArgumentNotNull("", null, new object()));
        }
    }
}