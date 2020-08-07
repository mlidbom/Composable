using System;
using System.Data.Common;
using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.Persistence.Common.AdoCE
{
    interface IDbConnectionPool<out TConnection, out TCommand>
        where TConnection : IPoolableConnection, IComposableDbConnection<TCommand>
        where TCommand : DbCommand
    {
        Task<TResult> UseConnectionAsyncFlex<TResult>(SyncOrAsync syncOrAsync, Func<TConnection, Task<TResult>> func);


        public TResult UseConnection<TResult>(Func<TConnection, TResult> func) =>
            UseConnectionAsyncFlex(SyncOrAsync.Sync, func.AsAsync()).SyncResult();

        public void UseConnection(Action<TConnection> action) =>
            UseConnectionAsyncFlex(SyncOrAsync.Sync, action.AsFunc().AsAsync()).SyncResult();

        public async Task UseConnectionAsync(Func<TConnection, Task> action) =>
            await UseConnectionAsyncFlex(SyncOrAsync.Async, action.AsFunc()).NoMarshalling();

        public async Task<TResult> UseConnectionAsync<TResult>(Func<TConnection, Task<TResult>> func) =>
            await UseConnectionAsyncFlex(SyncOrAsync.Async, func).NoMarshalling();



        public int ExecuteNonQuery(string commandText) =>
            UseConnection(connection => connection.ExecuteNonQuery(commandText));

        public object ExecuteScalar(string commandText) =>
            UseConnection(connection => connection.ExecuteScalar(commandText));

        public async Task<int> ExecuteNonQueryAsync(string commandText) =>
            await UseConnectionAsync(async connection => await connection.ExecuteNonQueryAsync(commandText).NoMarshalling()).NoMarshalling();

        public int PrepareAndExecuteNonQuery(string commandText) =>
            UseConnection(connection => connection.PrepareAndExecuteNonQuery(commandText));

        public object PrepareAndExecuteScalar(string commandText) =>
            UseConnection(connection => connection.PrepareAndExecuteScalar(commandText));

        public async Task<int> PrepareAndExecuteNonQueryAsync(string commandText) =>
            await UseConnectionAsync(async connection => await connection.PrepareAndExecuteNonQueryAsync(commandText).NoMarshalling()).NoMarshalling();

        public void UseCommand(Action<TCommand> action) =>
            UseConnection(connection => connection.UseCommand(action));

        public Task UseCommandAsync(Func<TCommand, Task> action) =>
            UseConnectionAsync(async connection => await connection.UseCommandAsync(action).NoMarshalling());

        public TResult UseCommand<TResult>(Func<TCommand, TResult> action) =>
            UseConnection(connection => connection.UseCommand(action));
    }
}
