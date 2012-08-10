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

            ContextCollection result = new ContextRunner(builder, new MyFormatter(), false).Run(builder.Contexts().Build());

            if (result.Failures() == null)
            {
                Assert.Fail("*****   Failed to execute some tests   *****");
            }

            if (result.Failures().Any())
            {
                Example failure = result.Failures().First();
                var position = failure.FullName().Substring(7)
                    .Split('.')
                    .Select((name, level) => "\t".Times(level) + name )
                    .Aggregate(Environment.NewLine + "at: ", (agg, curr) => agg + curr + Environment.NewLine);

                throw new SpecificationException(position, failure.Exception);
            }
        }

        public class MyFormatter : ConsoleFormatter, IFormatter, ILiveFormatter
        {
            void IFormatter.Write(ContextCollection contexts)
            {
                //Not calling base here lets us get rid of its noisy stack trace output so it does not obscure our thrown exceptions stacktrace.
                Console.WriteLine();
                if(contexts.Failures().Any())
                {
                    Console.WriteLine("*****   RUN SUMMARY   *****");
                }
                Console.WriteLine(base.Summary(contexts));
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

    public class SpecificationException : Exception
    {
        public SpecificationException(string position, Exception exception):base(position, exception)
        {            
        }
    }
}