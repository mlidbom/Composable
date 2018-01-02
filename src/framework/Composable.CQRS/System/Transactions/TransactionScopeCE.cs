using System;
using System.Threading.Tasks;
using System.Transactions;
using Composable.Contracts;
using JetBrains.Annotations;

namespace Composable.System.Transactions
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
            Contract.Argument.Assert(!typeof(Task).IsAssignableFrom(typeof(TResult)));
            using (var transaction = new TransactionScope(option, new TransactionOptions() { IsolationLevel = isolationLevel }))
            {
                result = action();
                transaction.Complete();
            }
            return result;
        }

        internal static void Execute([InstantHandle]Action action, TransactionScopeOption option = TransactionScopeOption.Required, IsolationLevel isolationLevel = IsolationLevel.Serializable)
        {
            using(var transaction = new TransactionScope(option, new TransactionOptions()
                                                                 {
                                                                     IsolationLevel = isolationLevel
                                                                 }, TransactionScopeAsyncFlowOption.Enabled))
            {
                action();
                transaction.Complete();
            }
        }
    }
}
