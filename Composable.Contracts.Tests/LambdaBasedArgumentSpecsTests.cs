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
            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Argument(() => nullString).NotNull())
                .Message.Should().Contain("nullString");

            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Argument(() => okString, () => nullString, () => notNullObject).NotNull())
                .Message.Should().Contain("nullString");

            Assert.Throws<StringIsEmptyContractViolationException>(() => Contract.Argument(() => okString, () => emptyString).NotNullOrEmpty())
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
        public void ThrowsIllegalArgumentAccessLambdaIfTheLambdaAccessesALiteral()
        {
            Assert.Throws<InvalidAccessorLambdaException>(() => Contract.Argument(() => ""));
            Assert.Throws<InvalidAccessorLambdaException>(() => Contract.Argument(() => 0));
        }

        [Test]
        public void ShouldRun50TestsIn1Millisecond() //The expression compilation stuff was worrying but this should be OK except for tight loops.
        {
            var notNullOrDefault = new object();

            Contract.Argument(() => notNullOrDefault).NotNullOrDefault();//Warm it up. various reflection things cost first time only...

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            for(var i = 0; i < 500; i++)
            {
                Contract.Argument(() => notNullOrDefault).NotNullOrDefault();
            }
            stopWatch.Elapsed.Should().BeLessOrEqualTo(10.Milliseconds());
        }


        private static void TestStringsForNullOrEmpty(string singleString)
        {
            Contract.Argument(() => singleString).NotNullOrEmpty();
        }


        private static void TestStringsForNullOrEmpty(string firstString, string secondString, string thirdString)
        {
            Contract.Argument(() => firstString, () => secondString, () => thirdString).NotNullOrEmpty();
        }
    }

    // ReSharper restore ConvertToConstant.Local
    // ReSharper restore ExpressionIsAlwaysNull
}
