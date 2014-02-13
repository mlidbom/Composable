using Composable.System.Linq;
using NUnit.Framework;
using FluentAssertions;

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
            ExpressionUtil.ExtractMemberName((ExpressionUtilTests me) => me.MyMember).Should().Be("MyMember");
        }

        [Test]
        public void CanExtractFromMemberAccessingLambdaWith2Parameters()
        {
            ExpressionUtil.ExtractMemberName((ExpressionUtilTests me, object irrelevant) => me.MyMember).Should().Be("MyMember");
        }

        private object MyMember{ get { throw new global::System.NotImplementedException(); } }
    }
}