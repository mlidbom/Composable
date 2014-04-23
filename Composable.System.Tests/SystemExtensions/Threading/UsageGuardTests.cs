using System;
using Composable.SystemExtensions.Threading;
using NUnit.Framework;

namespace Composable.Tests.SystemExtensions.Threading
{
    [TestFixture]
    public class UsageGuardTests
    {
        [Test]
        public void ThrowsIfUsedFromDifferentContext()
        {
            var alwaysInDifferentContext = new AlwayInDifferentContextUsageGuard();
            Assert.Throws<Exception>(() => alwaysInDifferentContext.AssertNoContextChangeOccurred(new object()));
        }

        [Test]
        public void DoesNotThrowIfCalledFromExcludedContext()
        {
            var alwaysInDifferentContext = new AlwayInDifferentContextUsageGuard();

            UsageGuard.RunInContextExcludedFromSingleUseRule(() => alwaysInDifferentContext.AssertNoContextChangeOccurred(new object()));
        }

        [Test]
        public void DoesNotThrowIfCalledFromExcludedContextInRecursiveReentrantFashion()
        {
            var alwaysInDifferentContext = new AlwayInDifferentContextUsageGuard();

            UsageGuard.RunInContextExcludedFromSingleUseRule(
                () =>
                {
                    UsageGuard.RunInContextExcludedFromSingleUseRule(() => { });
                    alwaysInDifferentContext.AssertNoContextChangeOccurred(new object());
                });
        }

        class AlwayInDifferentContextUsageGuard : UsageGuard
        {
            override protected void InternalAssertNoChangeOccurred(object guarded)
            {
                throw new Exception();
            }
        }
    }
}