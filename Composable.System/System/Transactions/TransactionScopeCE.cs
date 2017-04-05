using System;
using System.Transactions;

namespace Composable.System.Transactions
{
    public static class TransactionScopeCe
    {
        internal static void SupressAmbient(Action action) => Execute(action, TransactionScopeOption.Suppress);

        internal static void Execute(Action action, TransactionScopeOption option = TransactionScopeOption.Required, IsolationLevel isolationLevel = IsolationLevel.Serializable)
        {
            using(var transaction = new TransactionScope(option, new TransactionOptions() {IsolationLevel = isolationLevel}))
            {
                action();
                transaction.Complete();
            }
        }
    }
}
