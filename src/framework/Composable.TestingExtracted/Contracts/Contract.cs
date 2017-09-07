using System;
using System.Linq.Expressions;
using Composable.Testing.System;
using JetBrains.Annotations;

namespace Composable.Testing.Contracts
{
    public static class Contract
    {
        [AssertionMethod]
        internal static void AssertThat([AssertionCondition(AssertionConditionType.IS_TRUE)] bool assertion, string message)
        {
            if (message.IsNullOrWhiteSpace())
            {
                throw new ArgumentException(nameof(message));
            }
            if (!assertion)
            {
                throw new AssertionException(message);
            }
        }

        [AssertionMethod]
        internal static void AssertThat([AssertionCondition(AssertionConditionType.IS_TRUE)] bool assertion)
        {
            if (!assertion)
            {
                throw new AssertionException();
            }
        }
    }

    class AssertionException : Exception
    {
        public AssertionException(string message = null):base(message ?? string.Empty) { }
    }
}

// ReSharper restore UnusedParameter.Global
