using System;
using System.Data.Common;
using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Persistence.Common.AdoCE
{
    interface IComposableDbConnection
    {
        DbCommand CreateCommand();
    }

    interface IComposableDbConnection<out TCommand> : IComposableDbConnection
        where TCommand : DbCommand
    {
        new TCommand CreateCommand();

        void UseCommand(Action<TCommand> action)
        {
            using var command = CreateCommand();
            action(command);
        }

        TResult UseCommand<TResult>(Func<TCommand, TResult> action)
        {
            using var command = CreateCommand();
            return action(command);
        }

        async Task UseCommandAsync(Func<TCommand, Task> action)
        {
            await using var command = CreateCommand();
            await action(command).NoMarshalling();
        }

        public int ExecuteNonQuery(string commandText) => UseCommand(command => command.ExecuteNonQuery(commandText));

        public async Task<int> ExecuteNonQueryAsync(string commandText) => await UseCommand(command => command.ExecuteNonQueryAsync(commandText)).NoMarshalling();

        public object ExecuteScalar(string commandText) => UseCommand(command => command.ExecuteScalar(commandText));

        public Task<object> ExecuteScalarAsync(string commandText) => UseCommand(command => command.ExecuteScalarAsync(commandText));
    }
}
