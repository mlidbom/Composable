﻿using System.Linq;
using Composable.System.Linq;
using NUnit.Framework;

namespace Composable.Tests.Linq
{
    [TestFixture]
    public class SeqTests
    {
        [Test]
        public void CreateShouldEnumerateAllParamsInOrder()
        {
            var oneToTen = 1.Through(10);
            Assert.That(Seq.Create(oneToTen.ToArray()), Is.EquivalentTo(oneToTen));
        }
    }
}