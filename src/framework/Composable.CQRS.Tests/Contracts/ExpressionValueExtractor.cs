﻿using Composable.Contracts;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Composable.Tests.Contracts
{
    [TestFixture]
    public class ExpressionValueExtractorTests
    {
        readonly string _testString = "TestString";

        string TestString => _testString;

        readonly object _wrappedIntOne = 1;

        [Test]
        public void ExtractsValuesFromFieldAccessingLambdas()
        {
            var result = ContractsExpression.ExtractValue(() => _testString);
            Assert.That(result, Is.SameAs(_testString));

            var result2 = ContractsExpression.ExtractValue(() => _wrappedIntOne);
            Assert.That(result2, Is.SameAs(_wrappedIntOne));
        }

        [Test]
        public void ExtractsValueFromPropertyAccessLambda()
        {
            var result = ContractsExpression.ExtractValue(() => TestString);
            Assert.That(result, Is.SameAs(_testString));
        }

        [Test]
        public void ExtractsValueFromParameterAccess()
        {
            var result = ReturnExtractedParameterValue(_testString);
            Assert.That(result, Is.SameAs(_testString));
        }

        static TValue ReturnExtractedParameterValue<TValue>(TValue param)
        {
            return ContractsExpression.ExtractValue(() => param);
        }
    }
}