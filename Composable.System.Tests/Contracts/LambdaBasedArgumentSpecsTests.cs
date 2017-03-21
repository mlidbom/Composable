using Composable.Contracts;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.Contracts
{

  // ReSharper disable ConvertToConstant.Local
    // ReSharper disable ExpressionIsAlwaysNull
    [TestFixture]
    public class LambdaBasedArgumentSpecsTests
    {
        [Test]
        public void CorrectlyExtractsParameterNamesAndValues()
        {
            var notNullObject = new object();
            string okString = "okString";
            string emptyString = "";
            string nullString = null;
            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Argument(() => nullString).NotNull())
                .Message.Should().Contain(nameof(nullString));

            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Argument(() => okString, () => nullString, () => notNullObject).NotNull())
                .Message.Should().Contain(nameof(nullString));

            Assert.Throws<StringIsEmptyContractViolationException>(() => Contract.Argument(() => okString, () => emptyString).NotNullOrEmpty())
                .Message.Should().Contain(nameof(emptyString));

            Assert.Throws<ObjectIsNullContractViolationException>(() => TestStringsForNullOrEmpty(nullString))
                .Message.Should().Contain("singleString");

            Assert.Throws<ObjectIsNullContractViolationException>(() => TestStringsForNullOrEmpty(okString, nullString, emptyString))
                .Message.Should().Contain("secondString");

            Assert.Throws<StringIsEmptyContractViolationException>(() => TestStringsForNullOrEmpty(okString, emptyString, okString))
                .Message.Should().Contain("secondString");

            Assert.Throws<StringIsEmptyContractViolationException>(() => TestStringsForNullOrEmpty(okString, okString, emptyString))
                .Message.Should().Contain("thirdString");
        }

        [Test]
        public void ThrowsIllegalArgumentAccessLambdaIfTheLambdaAccessesALiteral()
        {
            Assert.Throws<InvalidAccessorLambdaException>(() => Contract.Argument(() => ""));
            Assert.Throws<InvalidAccessorLambdaException>(() => Contract.Argument(() => 0));
        }

        [Test, Category(TestCategories.PerformanceTest)]
        public void ShouldRun50TestsIn1Millisecond() //The expression compilation stuff was worrying but this should be OK except for tight loops.
        {
            var notNullOrDefault = new object();

            TimeAsserter.Execute(
                    action: () => Contract.Argument(() => notNullOrDefault).NotNullOrDefault(),
                    iterations: 500,
                    maxTotal: 10.Milliseconds().AdjustRuntimeToTestEnvironment(),
                    maxTries:3
            );
        }

        static void TestStringsForNullOrEmpty(string singleString)
        {
            Contract.Argument(() => singleString).NotNullOrEmpty();
        }

        static void TestStringsForNullOrEmpty(string firstString, string secondString, string thirdString)
        {
            Contract.Argument(() => firstString, () => secondString, () => thirdString).NotNullOrEmpty();
        }
    }

    // ReSharper restore ConvertToConstant.Local
    // ReSharper restore ExpressionIsAlwaysNull
}
