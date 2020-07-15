using System;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace Composable.Persistence.Oracle.SystemExtensions
{
    interface IOracleConnectionProvider
    {
        TResult UseConnection<TResult>(Func<OracleConnection, TResult> func);
        Task<TResult> UseConnectionAsync<TResult>(Func<OracleConnection,Task<TResult>> func);

        void UseConnection(Action<OracleConnection> action);
        Task UseConnectionAsync(Func<OracleConnection, Task> action);
    }
}
