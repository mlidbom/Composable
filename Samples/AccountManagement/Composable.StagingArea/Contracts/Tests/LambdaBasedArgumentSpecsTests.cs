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
            Assert.Throws<ObjectIsNullException>(() => Contract.Arguments(() => nullString).NotNull())
                .Message.Should().Contain("nullString");

            Assert.Throws<ObjectIsNullException>(() => Contract.Arguments(() => okString, () => nullString, () => notNullObject).NotNull())
                .Message.Should().Contain("nullString");

            Assert.Throws<StringIsEmptyException>(() => Contract.Arguments(() => okString, () => emptyString).NotNullOrEmpty())
                .Message.Should().Contain("emptyString");

            Assert.Throws<ObjectIsNullException>(() => TestStringsForNullOrEmpty(nullString))
                .Message.Should().Contain("singleString");

            Assert.Throws<ObjectIsNullException>(() => TestStringsForNullOrEmpty(okString, nullString, emptyString))
                .Message.Should().Contain("secondString");

            Assert.Throws<StringIsEmptyException>(() => TestStringsForNullOrEmpty(okString, emptyString, okString))
                .Message.Should().Contain("secondString");

            Assert.Throws<StringIsEmptyException>(() => TestStringsForNullOrEmpty(okString, okString, emptyString))
                .Message.Should().Contain("thirdString");
        }

        [Test]
        public void ThrowsIllegalArgumentAccessLambdaIfTheLambdaAcessesALiteral()
        {
            Assert.Throws < InvalidArgumentAccessorLambda>(() => Contract.Arguments(() => ""));
            Assert.Throws<InvalidArgumentAccessorLambda>(() => Contract.Arguments(() => 0));
        }

        [Test]
        public void ShouldRunAtLeast3TestsIn1Millisecond() //The expression compilation stuff was worrying but this should be OK except for very tight loops.
        {
            var notNullOrDefault = new object();
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            for (int i = 0; i < 300; i++)
            {
                Contract.Arguments(() => notNullOrDefault).NotNull();
            }
            stopWatch.Elapsed.Should().BeLessOrEqualTo(100.Milliseconds());
        }


        static void TestStringsForNullOrEmpty(string singleString)
        {
            Contract.Arguments(() => singleString).NotNullOrEmpty();
        }


        static void TestStringsForNullOrEmpty(string firstString, string secondString, string thirdString)
        {
            Contract.Arguments(() => firstString, () => secondString, () => thirdString).NotNullOrEmpty();
        }
    }
    // ReSharper restore ConvertToConstant.Local
    // ReSharper restore ExpressionIsAlwaysNull
}
