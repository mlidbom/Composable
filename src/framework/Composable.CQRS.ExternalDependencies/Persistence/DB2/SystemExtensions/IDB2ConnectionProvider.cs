using System;
using System.Threading.Tasks;
using IBM.Data.DB2.Core;

namespace Composable.Persistence.DB2.SystemExtensions
{
    interface IComposableDB2ConnectionProvider
    {
        TResult UseConnection<TResult>(Func<ComposableDB2Connection, TResult> func);
        Task<TResult> UseConnectionAsync<TResult>(Func<ComposableDB2Connection,Task<TResult>> func);

        void UseConnection(Action<ComposableDB2Connection> action);
        Task UseConnectionAsync(Func<ComposableDB2Connection, Task> action);
    }
}
