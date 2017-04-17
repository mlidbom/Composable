using System;
using FluentAssertions;
using NUnit.Framework;

// ReSharper disable UnusedMember.Global

// ReSharper disable InconsistentNaming

namespace Composable
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
            var exception = Assert.Throws<SpecificationException>(() => new reports_first_failure().ValidateSpec());
            exception.InnerException.Message.Should().Be("first error");
        }
    }

    public class working_spec : nspec
    {
        public void this_works()
        {
            it["true is true"] = () => true.Should().Be(true);
        }
    }

    [Ignore("SHould fail when executed and actual test is done by checking that it fails whith an appropriate message.")]
    public class specification_of_xfiles_math : nspec
    {
        public void when_using_strange_math()
        {
            it["1 equals 2"] = () => 1.Should().Be(2);
        }
    }

    [Ignore("SHould fail when executed and actual test is done by checking that it fails whith an appropriate message.")]
    public class fails_in_before_all : nspec
    {
        public void before_all()
        {
            throw new Exception("first error");
        }

        public void this_works()
        {
            it["true is true"] = () => true.Should().Be(true);
        }
    }

    [Ignore("SHould fail when executed and actual test is done by checking that it fails whith an appropriate message.")]
    public class fails_in_before_each : nspec
    {
        public void before_each()
        {
            throw new Exception("second error");
        }

        public void this_works()
        {
            it["true is true"] = () => true.Should().Be(true);
        }
    }

    [Ignore("SHould fail when executed and actual test is done by checking that it fails whith an appropriate message.")]
    public class reports_first_failure : nspec
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
            it["true is true"] = () => true.Should().Be(true);
        }
    }

    [Ignore("SHould fail when executed and actual test is done by checking that it fails whith an appropriate message.")]
    public class dots : nspec
    {
        public void any_time()
        {
            context["level1 . level1 ."] =
                () => context["level2 . level2 ."] =
                          () => it["I . Use . Three . Dots"] = () => throw new Exception();
        }
    }
}
