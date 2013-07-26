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
            Assert.Throws<SpecificationException>(() => new fails_in_before_each().ValidateSpec());
        }

        [Test]
        public void throws_on_failing_before_all()
        {
            Assert.Throws<SpecificationException>(() => new fails_in_before_all().ValidateSpec());
        }

        [Test]
        public void reports_first_error()
        {
            var exception= Assert.Throws<SpecificationException>(() => new reports_first_failure().ValidateSpec());
            exception.InnerException.Message.should_be("first error");
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
        public void before_all()
        {
            throw new Exception("first error");
        }

        public void this_works()
        {
            it["true is true"] = () => true.should_be(true);
        }
    }

    [Ignore]
    public class fails_in_before_each : NSpec.NUnit.nspec
    {
        public void before_each()
        {
            throw new Exception("second error");
        }

        public void this_works()
        {
            it["true is true"] = () => true.should_be(true);
        }
    }

    [Ignore]
    public class reports_first_failure : NSpec.NUnit.nspec
    {
        public void before_all()
        {
            throw new Exception("first error");
        }

        public void before_each()
        {
            throw new Exception("second error");
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
