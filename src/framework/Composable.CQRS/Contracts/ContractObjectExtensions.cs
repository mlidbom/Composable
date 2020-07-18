using System;

namespace Composable.Contracts
{
    static class ContractObjectExtensions
    {
        public static T Assert<T>(this T @this, Func<T, bool> assertion, string message = "")
        {
            Contract.Assert.That(assertion(@this), message);
            return @this;
        }

        public static T Assert<T>(this T @this, bool assert, string message = "")
        {
            Contract.Assert.That(assert, message);
            return @this;
        }
    }
}
