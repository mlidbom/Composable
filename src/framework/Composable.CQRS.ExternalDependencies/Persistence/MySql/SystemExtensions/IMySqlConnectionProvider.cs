using System;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Composable.Persistence.MySql.SystemExtensions
{
    interface IMySqlConnectionProvider
    {
        TResult UseConnection<TResult>(Func<MySqlConnection, TResult> func);
        Task<TResult> UseConnectionAsync<TResult>(Func<MySqlConnection,Task<TResult>> func);

        void UseConnection(Action<MySqlConnection> action);
        Task UseConnectionAsync(Func<MySqlConnection, Task> action);
    }
}
