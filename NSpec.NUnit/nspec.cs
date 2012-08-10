using System;
using NSpec.Domain.Formatters;
using NUnit.Framework;
using NSpec.Domain;
using NSpec;
using System.Linq;

namespace NSpec.NUnit
{
    /// <summary>
    /// This class acts as as shim between NSpec and NUnit. If you inherit it instead of <see cref="NSpec.nspec"/> you can work as usual with nspec and nunit will execute your tests for you.
    /// </summary>
    [TestFixture]
// ReSharper disable InconsistentNaming
    public abstract class nspec : NSpec.nspec
// ReSharper restore InconsistentNaming
    {
        [Test]
        public void ValidateSpec()
        {
            var finder = new SpecFinder(new[] { GetType() });
            var builder = new ContextBuilder(finder, new DefaultConventions());

            ContextCollection result = new ContextRunner(builder, new ConsoleFormatter(), false).Run(builder.Contexts().Build());

            if (result.Failures() == null)
            {
                Assert.Fail("Failed to execute some tests");
            }

            if (result.Failures().Any())
            {
                throw new Exception("The stacktrace for the first failure", result.Failures().First().Exception);
            }
        }
    }
}