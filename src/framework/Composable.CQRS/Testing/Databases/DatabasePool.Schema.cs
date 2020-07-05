using System.Collections.Generic;
using System.Linq;
using Composable.System.Linq;
using Composable.System.Transactions;

namespace Composable.Testing.Databases
{
    static class DatabaseExtensions
    {
        internal static string Name(this DatabasePool.Database @this) => $"{DatabasePool.PoolDatabaseNamePrefix}{@this.Id:0000}";
        internal static string ConnectionString(this DatabasePool.Database @this, DatabasePool pool) => pool.ConnectionStringForDbNamed(@this.Name());
    }

    partial class DatabasePool
    {
        
    }
}