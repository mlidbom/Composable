using System;
using System.Transactions;
using JetBrains.Annotations;

namespace Composable.SystemCE.Transactions
{
    public static class TransactionScopeCe
    {
        internal static void SuppressAmbientAndExecuteInNewTransaction(Action action) => SuppressAmbient(() =>Execute(action));

        internal static TResult SuppressAmbientAndExecuteInNewTransaction<TResult>([InstantHandle]Func<TResult> action) => SuppressAmbient(() => Execute(action));

        internal static void SuppressAmbient(Action action) => Execute(action, TransactionScopeOption.Suppress);

        internal static TResult SuppressAmbient<TResult>([InstantHandle]Func<TResult> action) => Execute(action, TransactionScopeOption.Suppress);

        internal static TResult Execute<TResult>([InstantHandle]Func<TResult> action, TransactionScopeOption option = TransactionScopeOption.Required, IsolationLevel isolationLevel = IsolationLevel.Serializable)
        {
            TResult result;
            using (var transaction = CreateScope(option, isolationLevel))
            {
                result = action();
                transaction.Complete();
            }
            return result;
        }

        internal static void Execute([InstantHandle]Action action, TransactionScopeOption option = TransactionScopeOption.Required, IsolationLevel isolationLevel = IsolationLevel.Serializable)
        {
            using var transaction = CreateScope(option, isolationLevel);
            action();
            transaction.Complete();
        }

        static TransactionScope CreateScope(TransactionScopeOption options, IsolationLevel isolationLevel) =>
            new TransactionScope(options,
                                 new TransactionOptions
                                 {
                                     IsolationLevel = isolationLevel
                                 },
                                 TransactionScopeAsyncFlowOption.Enabled);
    }
}
