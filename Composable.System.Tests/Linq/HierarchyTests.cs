using System.Collections.Generic;
using System.Linq;
using Composable.GenericAbstractions.Hierarchies;
using Composable.System.Linq;
using NUnit.Framework;

namespace Composable.Tests.Linq
{
    [TestFixture]
    public class HierarchyTests
    {
        class Hierarchical
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

        class Person : IHierarchy<Person>
        {
            public IList<Person> Children = new List<Person>();
            IEnumerable<Person> IHierarchy<Person>.Children => Children;
        }


        [Test]
        public void FlatteningAHierarchicalTypeShouldWork()
        {
            var family = new Person
                             {
                                 Children = new List<Person>
                                                {
                                                    new Person
                                                        {
                                                            Children = new List<Person>
                                                                           {
                                                                               new Person(),
                                                                               new Person()
                                                                           }
                                                        },
                                                    new Person()
                                                }
                             };
            var familyRegister = family.Flatten();
            Assert.That(familyRegister.Count(), Is.EqualTo(5), "Should have 5 persons in the list");
            Assert.That(familyRegister.Count(), Is.EqualTo(5), "Should have 5 unique persons in the list");
        }
    }
}