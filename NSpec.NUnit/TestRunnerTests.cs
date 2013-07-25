using System;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace NSpec.NUnit
{
    [TestFixture]
    public class TestRunnerTests
    {
        [Test]
        public void throws_on_failing_spec()
        {
            Assert.Throws<SpecificationException>(() => new specification_of_xfiles_math().ValidateSpec());
        }

        [Test]
        public void works_when_it_works()
        {
            new working_spec().ValidateSpec();
        }

        [Test]
        public void throws_on_failing_before_each()
        {
            Assert.Throws<SpecificationException>(() => new fails_in_before_all().ValidateSpec());
        }

        [Test]
        public void handles_dots_without_crazy_formatting()
        {
            var exception = Assert.Throws<SpecificationException>(() => new dots().ValidateSpec());
            exception.Message.should_be(
@"
at: dots
	any time
		level1 . level1 .
			level2 . level2 .
				I . Use . Three . Dots
");
        }
    }

    [Ignore]
    public class working_spec : NSpec.NUnit.nspec
    {
        public void this_works()
        {
            it["true is true"] = () => true.should_be(true);
        }
    }

    [Ignore]
    public class specification_of_xfiles_math : NSpec.NUnit.nspec
    {
        public void when_using_strange_math()
        {
            it["1 equals 2"] = () => 1.should_be(2);
        }
    }

    [Ignore]
    public class fails_in_before_all : NSpec.NUnit.nspec
    {
        public void before_each()
        {
            throw new Exception();
        }

        public void this_works()
        {
            it["true is true"] = () => true.should_be(true);
        }
    }

    [TestFixture, Ignore]
    public class dots : NUnit.nspec
    {
        public void any_time()
        {
            context["level1 . level1 ."] =
                () =>
                {
                    context["level2 . level2 ."] =
                        () =>
                        {
                            it["I . Use . Three . Dots"] = () => { throw new Exception(); };
                        };
                };
        }
    }
}
