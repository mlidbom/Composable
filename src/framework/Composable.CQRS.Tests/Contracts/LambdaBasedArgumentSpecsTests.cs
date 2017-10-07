using Composable.Contracts;
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
            Assert.Throws<ObjectIsNullContractViolationException>(() => OldContract.Argument(() => nullString).NotNull())
                .Message.Should().Contain(nameof(nullString));

            Assert.Throws<ObjectIsNullContractViolationException>(() => OldContract.Argument(() => okString, () => nullString, () => notNullObject).NotNull())
                .Message.Should().Contain(nameof(nullString));

            Assert.Throws<StringIsEmptyContractViolationException>(() => OldContract.Argument(() => okString, () => emptyString).NotNullOrEmpty())
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
            Assert.Throws<InvalidAccessorLambdaException>(() => OldContract.Argument(() => ""));
            Assert.Throws<InvalidAccessorLambdaException>(() => OldContract.Argument(() => 0));
        }

        static void TestStringsForNullOrEmpty(string singleString)
        {
            OldContract.Argument(() => singleString).NotNullOrEmpty();
        }

        static void TestStringsForNullOrEmpty(string firstString, string secondString, string thirdString)
        {
            OldContract.Argument(() => firstString, () => secondString, () => thirdString).NotNullOrEmpty();
        }
    }

    // ReSharper restore ConvertToConstant.Local
    // ReSharper restore ExpressionIsAlwaysNull
}
