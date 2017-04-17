using System;
using System.Transactions;

namespace Composable.System.Transactions
{
    public static class TransactionScopeCe
    {
        internal static void SupressAmbient(Action action) => Execute(action, TransactionScopeOption.Suppress);

        internal static TResult SupressAmbient<TResult>(Func<TResult> action) => Execute(action, TransactionScopeOption.Suppress);



        internal static TResult Execute<TResult>(Func<TResult> action, TransactionScopeOption option = TransactionScopeOption.Required, IsolationLevel isolationLevel = IsolationLevel.Serializable)
        {
            TResult result;
            using (var transaction = new TransactionScope(option, new TransactionOptions() { IsolationLevel = isolationLevel }))
            {
                result = action();
                transaction.Complete();
            }
            return result;
        }

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
