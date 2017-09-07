using System;
using Composable.Testing.System;
using JetBrains.Annotations;

namespace Composable.Testing.Contracts
{
    static class ContractAssertion
    {
        [AssertionMethod]
        internal static void That(this IContractAssertion @this, [AssertionCondition(AssertionConditionType.IS_TRUE)] bool assertion, string message)
        {
            if(message.IsNullOrWhiteSpace())
            {
                throw new ArgumentException(nameof(message));
            }
            if (!assertion)
            {
                throw new AssertionException(@this.InspectionType, message);
            }
        }
    }
}