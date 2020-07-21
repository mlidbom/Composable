using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Composable.Testing
{
    [TestFixture, TestFixtureSource(typeof(PluggableComponentsTestFixtureSource))]
    public class DuplicateByPluggableComponentTest
    {
        public DuplicateByPluggableComponentTest(string _) {}
    }

    public class PluggableComponentsTestFixtureSource : IEnumerable<string>
    {
        static readonly List<string> Dimensions = ConfigurationBasedDuplicateByDimensionsAttribute.CreateDimensions().ToList();
        public IEnumerator<string> GetEnumerator() => Dimensions.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
