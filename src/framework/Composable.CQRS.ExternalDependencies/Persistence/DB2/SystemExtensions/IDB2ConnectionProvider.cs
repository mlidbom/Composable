using System;
using System.Threading.Tasks;
using IBM.Data.DB2.Core;

namespace Composable.Persistence.DB2.SystemExtensions
{
    interface IDB2ConnectionProvider
    {
        TResult UseConnection<TResult>(Func<DB2Connection, TResult> func);
        Task<TResult> UseConnectionAsync<TResult>(Func<DB2Connection,Task<TResult>> func);

        void UseConnection(Action<DB2Connection> action);
        Task UseConnectionAsync(Func<DB2Connection, Task> action);
    }
}
