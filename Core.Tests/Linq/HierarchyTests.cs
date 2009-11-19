using System.Linq;
using NUnit.Framework;
using Void.Linq;

namespace Core.Tests.Linq
{
    [TestFixture]
    public class HierarchyTests
    {
        private class Hierarchical
        {
            public Hierarchical[] Children = new Hierarchical[0];
        }

        [Test]
        public void ShouldReturnAllInstancesInGraphWithoutDuplicates()
        {
            var root1 = new Hierarchical
                        {
                            Children = new[]
                                       {
                                           new Hierarchical
                                           {
                                               Children = new[]
                                                          {
                                                              new Hierarchical(),
                                                              new Hierarchical()
                                                          }
                                           },
                                           new Hierarchical()
                                       }
                        };
            var root2 = new Hierarchical
                        {
                            Children = new[]
                                       {
                                           new Hierarchical
                                           {
                                               Children = new[]
                                                          {
                                                              new Hierarchical(),
                                                              new Hierarchical()
                                                          }
                                           },
                                           new Hierarchical()
                                       }
                        };

            var flattened = Seq.Create(root1, root2).FlattenHierarchy(root => root.Children);
            Assert.That(flattened.Count(), Is.EqualTo(10)); //Ensures no duplicates
            Assert.That(flattened.Distinct().Count(), Is.EqualTo(10)); //Ensures all objects are there.
        }
    }
}