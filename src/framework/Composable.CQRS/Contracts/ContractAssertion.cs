﻿using System;
using System.Diagnostics.CodeAnalysis;
using Composable.System;
using JetBrains.Annotations;

namespace Composable.Contracts
{
    static class ContractAssertion
    {
        [AssertionMethod]
        internal static void That(this IContractAssertion @this, [AssertionCondition(AssertionConditionType.IS_TRUE)] [DoesNotReturnIf(false)]bool assertion, string message)
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