using System;

namespace Composable.SystemExtensions.Threading
{
    public class ComponentUsedByMultipleTransactionsException : Exception
    {
        public ComponentUsedByMultipleTransactionsException(Type componentType) : base($"Using a {componentType.FullName} in multiple transactions is not safe. It makes you vulnerable to hard to debug concurrency issues and is therefore not allowed.") {}
    }
}
