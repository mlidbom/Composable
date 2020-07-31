using System;
using System.Data.Common;
using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Persistence.Common
{
    interface IDbConnectionPool<out TConnection, out TCommand>
        where TConnection : IPoolableConnection, IComposableDbConnection<TCommand>
        where TCommand : DbCommand
    {
        Task<TResult> UseConnectionAsyncFlex<TResult>(AsyncMode syncOrAsync, Func<TConnection, Task<TResult>> func);


        public TResult UseConnection<TResult>(Func<TConnection, TResult> func) =>
            UseConnectionAsyncFlex(AsyncMode.Sync, func.AsAsync()).AwaiterResult();

        public void UseConnection(Action<TConnection> action) =>
            UseConnectionAsyncFlex(AsyncMode.Sync, action.AsFunc().AsAsync()).AwaiterResult();

        public async Task UseConnectionAsync(Func<TConnection, Task> action) =>
            await UseConnectionAsyncFlex(AsyncMode.Async, action.AsFunc()).NoMarshalling();

        public async Task<TResult> UseConnectionAsync<TResult>(Func<TConnection, Task<TResult>> func) =>
            await UseConnectionAsyncFlex(AsyncMode.Async, func).NoMarshalling();



        public int ExecuteNonQuery(string commandText) =>
            UseConnection(connection => connection.ExecuteNonQuery(commandText));

        public object ExecuteScalar(string commandText) =>
            UseConnection(connection => connection.ExecuteScalar(commandText));

        public async Task<int> ExecuteNonQueryAsync(string commandText) =>
            await UseConnectionAsync(async connection => await connection.ExecuteNonQueryAsync(commandText).NoMarshalling()).NoMarshalling();


        public void UseCommand(Action<TCommand> action) =>
            UseConnection(connection => connection.UseCommand(action));

        public Task UseCommandAsync(Func<TCommand, Task> action) =>
            UseConnectionAsync(async connection => await connection.UseCommandAsync(action).NoMarshalling());

        public TResult UseCommand<TResult>(Func<TCommand, TResult> action) =>
            UseConnection(connection => connection.UseCommand(action));
    }
}
