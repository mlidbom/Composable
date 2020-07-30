using System;
using System.Threading.Tasks;

namespace Composable.Persistence.DB2.SystemExtensions
{
    interface IDB2ConnectionProvider
    {
        TResult UseConnection<TResult>(Func<ComposableDB2Connection, TResult> func);
        Task<TResult> UseConnectionAsync<TResult>(Func<ComposableDB2Connection,Task<TResult>> func);

        void UseConnection(Action<ComposableDB2Connection> action);
        Task UseConnectionAsync(Func<ComposableDB2Connection, Task> action);
    }
}
