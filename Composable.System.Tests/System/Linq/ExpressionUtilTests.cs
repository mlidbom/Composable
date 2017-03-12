using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.System.Linq
{
    [TestFixture]
    public class ExpressionUtilTests
    {
        [Test]
        public void CanExtractFromMemberAccessingLambdaWithNoParameter()
        {
            ExpressionUtil.ExtractMemberName(() => MyMember).Should().Be("MyMember");
        }

        [Test]
        public void CanExtractFromMemberAccessingLambdaWithParameter()
        {
            ExpressionUtil.ExtractMemberName((ExpressionUtilTests me) => MyMember).Should().Be("MyMember");
        }

        [Test]
        public void CanExtractFromMemberAccessingLambdaWith2Parameters()
        {
            ExpressionUtil.ExtractMemberName((ExpressionUtilTests me, object irrelevant) => MyMember).Should().Be("MyMember");
        }

        static object MyMember //ncrunch: no coverage
            => throw new global::System.NotImplementedException();
    }
}