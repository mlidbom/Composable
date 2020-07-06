using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Composable.Persistence.SqlServer.SystemExtensions
{
    interface ISqlServerConnectionProvider
    {
        TResult UseConnection<TResult>(Func<SqlConnection, TResult> func);
        Task<TResult> UseConnectionAsync<TResult>(Func<SqlConnection,Task<TResult>> func);

        void UseConnection(Action<SqlConnection> action);
        Task UseConnectionAsync(Func<SqlConnection, Task> action);
    }
}
