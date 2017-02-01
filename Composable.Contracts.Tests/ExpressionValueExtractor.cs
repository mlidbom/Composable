using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    [TestFixture]
    public class ExpressionValueExtractorTests
    {
        string _testString = "TestString";

        string TestString { get { return _testString; } }

        object WrappedIntOne = 1;

        [Test]
        public void ExtractsValuesromFieldAccessingLambdas()
        {
            var result = ContractsExpression.ExtractValue(() => _testString);
            Assert.That(result, Is.SameAs(_testString));

            var result2 = ContractsExpression.ExtractValue(() => WrappedIntOne);
            Assert.That(result2, Is.SameAs(WrappedIntOne));
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