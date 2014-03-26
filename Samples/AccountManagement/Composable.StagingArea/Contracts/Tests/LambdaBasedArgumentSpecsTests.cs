using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Contracts.Tests
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
            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Arguments(() => nullString).NotNull())
                .Message.Should().Contain("nullString");

            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Arguments(() => okString, () => nullString, () => notNullObject).NotNull())
                .Message.Should().Contain("nullString");

            Assert.Throws<StringIsEmptyContractViolationException>(() => Contract.Arguments(() => okString, () => emptyString).NotNullOrEmpty())
                .Message.Should().Contain("emptyString");

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
        public void ThrowsIllegalArgumentAccessLambdaIfTheLambdaAcessesALiteral()
        {
            Assert.Throws<InvalidAccessorLambdaException>(() => Contract.Arguments(() => ""));
            Assert.Throws<InvalidAccessorLambdaException>(() => Contract.Arguments(() => 0));
        }

        [Test]
        public void ShouldRunAtLeast2TestsIn1Millisecond() //The expression compilation stuff was worrying but this should be OK except for tight loops.
        {
            var notNullOrDefault = new object();
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            for(int i = 0; i < 200; i++)
            {
                Contract.Arguments(() => notNullOrDefault).NotNull();
            }
            stopWatch.Elapsed.Should().BeLessOrEqualTo(100.Milliseconds());
        }


        private static void TestStringsForNullOrEmpty(string singleString)
        {
            Contract.Arguments(() => singleString).NotNullOrEmpty();
        }


        private static void TestStringsForNullOrEmpty(string firstString, string secondString, string thirdString)
        {
            Contract.Arguments(() => firstString, () => secondString, () => thirdString).NotNullOrEmpty();
        }
    }

    // ReSharper restore ConvertToConstant.Local
    // ReSharper restore ExpressionIsAlwaysNull
}
