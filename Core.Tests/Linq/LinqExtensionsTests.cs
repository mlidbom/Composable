using System;
using System.Collections.Generic;
using NUnit.Framework;
using Void.Linq;

namespace Core.Tests.Linq
{
    [TestFixture]
    public class LinqExtensionsTests
    {
        [Test]
        public void FlattenShouldIterateAllNestedCollectionInstances()
        {
            var nestedInts = new List<List<int>>()
                             {
                                 new List<int> {1, }, 
                                 new List<int>{2,3},
                                 new List<int>{4,5,6,7}
                             };

            var flattened = LinqExtensions.Flatten<List<int>, int>(nestedInts);
            Assert.That(flattened, Is.EquivalentTo(1.Through(7)));
        }
    }
}