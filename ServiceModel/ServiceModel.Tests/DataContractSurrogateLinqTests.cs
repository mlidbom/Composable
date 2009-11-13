using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Void.Linq;
using Void.ServiceModel;
using System.Linq;

namespace ServiceModel
{
    [TestFixture]
    public class DataContractSurrogateLinqTests
    {
        //todo: extract and reuse somehow
        public class CollectionsEqualConstraint : CollectionConstraint
        {
            public CollectionsEqualConstraint(IEnumerable actual) : base(actual)
            {   
            }

            public override void WriteDescriptionTo(MessageWriter writer)
            {
                writer.WriteExpectedValue(actual);
            }


            protected override bool doMatch(IEnumerable collection)
            {
                var zipped =((IEnumerable) actual).Cast<object>().Zip(collection.Cast<object>());
                return !zipped.Any( current => !Equals(current.First, current.Second));
            }
        }

        [Test]
        public void MethodsShouldBeCalledInOrder()
        {
            var expected = Seq.Create("first", "second");

            var result = new List<string>();
            var uut = new DataContractSurrogateLink(new DataContractSurrogateAdapterFunctional
                                                    {
                                                        GetCustomDataToExportFunc   = (_,__)=>
                                                                                      {
                                                                                          result.Add("first");
                                                                                          return null;
                                                                                      }
                                                    },
                                                    new DataContractSurrogateAdapterFunctional
                                                    {
                                                        GetCustomDataToExportFunc = (_, __) =>
                                                        {
                                                            result.Add("second");
                                                            return null;
                                                        }
                                                    });

            uut.GetCustomDataToExport(null, null);
            Assert.That(expected, new CollectionsEqualConstraint(result));
        }
    }
}