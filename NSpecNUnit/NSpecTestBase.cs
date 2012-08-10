using System;
using NSpec.Domain.Formatters;
using NUnit.Framework;
using NSpec.Domain;
using NSpec;
using System.Linq;

namespace NSpecNUnit
{
    [TestFixture]
    public abstract class NSpecTestBase : nspec
    {
        [Test]
        public void ValidateSpec()
        {
            var finder = new SpecFinder(new[] { GetType() });
            var builder = new ContextBuilder(finder, new DefaultConventions());

            ContextCollection result = new ContextRunner(builder, new MyFormatter(), false).Run(builder.Contexts().Build());

            if (result.Failures() == null)
            {
                Assert.Fail("Failed to execute some tests");
            }

            if (result.Failures().Any())
            {
                throw new Exception("The stacktrace for the first failure", result.Failures().First().Exception);
            }
        }

        public class MyFormatter : ConsoleFormatter, IFormatter, ILiveFormatter
        {
            void IFormatter.Write(ContextCollection contexts)
            {
                //Writing nothing here gets rid of the error summary that we don't want anyway since we replace it with the thrown info.
                //base.Write(contexts);
            }

            void ILiveFormatter.Write(Context context)
            {
                base.Write(context);
            }

            void ILiveFormatter.Write(Example example, int level)
            {
                base.Write(example, level);
            }
        }
    }
}