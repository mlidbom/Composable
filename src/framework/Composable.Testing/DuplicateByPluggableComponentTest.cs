using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Composable.Testing
{
    [TestFixture, TestFixtureSource(typeof(PluggableComponentsTestFixtureSource))]
    public class DuplicateByPluggableComponentTest
    {
        public DuplicateByPluggableComponentTest(string pluggableComponentsColonSeparated) {}
    }

    class PluggableComponentsTestFixtureSource : IEnumerable<string>
    {
        static readonly List<string> Dimensions = CreateDimensions().ToList();
        public IEnumerator<string> GetEnumerator() => Dimensions.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        const string TestUsingPluggableComponentCombinations = "TestUsingPluggableComponentCombinations";
        static string[] CreateDimensions()
        {
            try
            {
                return File.ReadAllLines(TestUsingPluggableComponentCombinations)
                           .Select(@this => @this.Trim())
                           .Where(line => !string.IsNullOrEmpty(line))
                           .Where(line => !line.StartsWith("#", StringComparison.InvariantCulture))
                           .ToArray();
            }
            catch(Exception e)
            {
                return new[] {e.ToString()};
            }
        }
    }
}
