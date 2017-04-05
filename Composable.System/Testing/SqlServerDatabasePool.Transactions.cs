using System;
using Composable.System.Transactions;

namespace Composable.Testing
{
    sealed partial class SqlServerDatabasePool
    {
        static void RunInIsolatedTransaction(Action action)
            => TransactionScopeCe.SupressAmbient(
                () => TransactionScopeCe.Execute(action));
    }
}
