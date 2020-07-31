using System;
using System.Threading.Tasks;

namespace Composable.Persistence.Oracle.SystemExtensions
{
    interface IOracleConnectionProvider
    {
        TResult UseConnection<TResult>(Func<ComposableOracleConnection, TResult> func);
        Task<TResult> UseConnectionAsync<TResult>(Func<ComposableOracleConnection,Task<TResult>> func);

        void UseConnection(Action<ComposableOracleConnection> action);
        Task UseConnectionAsync(Func<ComposableOracleConnection, Task> action);
    }
}
