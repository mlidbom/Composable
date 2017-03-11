using Composable.Contracts;
using NUnit.Framework;

namespace Composable.Tests.Contracts
{
    [TestFixture]
    public class ExpressionValueExtractorTests
    {
        string _testString = "TestString";

        string TestString { get { return _testString; } }

        object _wrappedIntOne = 1;

        [Test]
        public void ExtractsValuesromFieldAccessingLambdas()
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

        TValue ReturnExtractedParameterValue<TValue>(TValue param)
        {
            return ContractsExpression.ExtractValue(() => param);
        }
    }
}