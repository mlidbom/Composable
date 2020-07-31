using System;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Composable.Persistence.MySql.SystemExtensions
{
    interface IMySqlConnectionProvider
    {
        TResult UseConnection<TResult>(Func<ComposableMySqlConnection, TResult> func);
        Task<TResult> UseConnectionAsync<TResult>(Func<ComposableMySqlConnection,Task<TResult>> func);

        void UseConnection(Action<ComposableMySqlConnection> action);
        Task UseConnectionAsync(Func<ComposableMySqlConnection, Task> action);
    }
}
