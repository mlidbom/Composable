﻿using Composable.Contracts;
using FluentAssertions;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

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
            var okString = "okString";
            var emptyString = "";
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
