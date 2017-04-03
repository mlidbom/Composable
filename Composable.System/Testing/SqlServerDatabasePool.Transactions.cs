using System;
using System.Transactions;

namespace Composable.Testing
{
    sealed partial class SqlServerDatabasePool
    {
        static void RunInIsolatedTransaction(Action action)
        {
            using(new TransactionScope(TransactionScopeOption.Suppress))//we do not want to participate in any transactions started by our clients here.
            using(var transaction = new TransactionScope(TransactionScopeOption.Required,
                                                         new TransactionOptions
                                                         {
                                                             IsolationLevel = IsolationLevel.Serializable
                                                         }))
            {
                action();
                transaction.Complete();
            }
        }
    }
}
