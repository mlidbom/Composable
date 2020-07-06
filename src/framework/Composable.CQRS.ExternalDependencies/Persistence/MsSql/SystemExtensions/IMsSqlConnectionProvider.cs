using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Composable.Persistence.MsSql.SystemExtensions
{
    interface IMsSqlConnectionProvider
    {
        TResult UseConnection<TResult>(Func<SqlConnection, TResult> func);
        Task<TResult> UseConnectionAsync<TResult>(Func<SqlConnection,Task<TResult>> func);

        void UseConnection(Action<SqlConnection> action);
        Task UseConnectionAsync(Func<SqlConnection, Task> action);
    }
}
