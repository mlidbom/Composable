using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Composable.Persistence.MsSql.SystemExtensions
{
    interface IMsSqlConnectionProvider
    {
        TResult UseConnection<TResult>(Func<ComposableMsSqlConnection, TResult> func);
        Task<TResult> UseConnectionAsync<TResult>(Func<ComposableMsSqlConnection,Task<TResult>> func);

        void UseConnection(Action<ComposableMsSqlConnection> action);
        Task UseConnectionAsync(Func<ComposableMsSqlConnection, Task> action);
    }
}
