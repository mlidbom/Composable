﻿using Composable.SystemCE.LinqCE;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.SystemCE.LinqCE
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

        static object MyMember => throw new global::System.Exception(); //ncrunch: no coverage
    }
}