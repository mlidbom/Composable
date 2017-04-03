using System;
using System.Transactions;

namespace Composable.Testing
{
    sealed partial class SqlServerDatabasePool
    {
        static void RunInIsolatedTransaction(Action action)
        {
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
