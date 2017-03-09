#region usings

using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

#endregion

namespace Core.Tests.Linq
{
    [TestFixture]
    public class SimpleIndexingTests
    {
        [Test]
        public void ShouldIndexCorrectly()
        {
            var indexesEqualValues = 0.Through(9).ToList();

            Assert.That(indexesEqualValues.Second(), Is.EqualTo(1));
            Assert.That(indexesEqualValues.Third(), Is.EqualTo(2));
            Assert.That(indexesEqualValues.Fourth(), Is.EqualTo(3));
            Assert.That(indexesEqualValues.Fifth(), Is.EqualTo(4));
            Assert.That(indexesEqualValues.Sixth(), Is.EqualTo(5));
            Assert.That(indexesEqualValues.Seventh(), Is.EqualTo(6));
            Assert.That(indexesEqualValues.Eighth(), Is.EqualTo(7));
            Assert.That(indexesEqualValues.Ninth(), Is.EqualTo(8));
        }

        [Test]
        public void ThrowsExceptionIfArgumentIsNull()
        {
            List<int> indexesEqualValues = null;

            indexesEqualValues.Invoking( me => me.Second()).ShouldThrow<Exception>()
                .WithMessage("Argument: me");
        }
    }
}