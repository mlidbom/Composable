using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Composable.Logging;
using Composable.SystemCE;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Persistence.Common
{
    interface IComposableDbConnection
    {
        DbCommand CreateCommand();

        //Urgent: Remove this as soon as all persistence layers implement this interface so we can migrate to using CreateCommand.
        IDbConnection Connection { get; }
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

    static class DbCommandCE
    {
        public static object ExecuteScalar(this DbCommand @this, string commandText) =>
            @this.SetCommandText(commandText).ExecuteScalar();

        public static async Task<object> ExecuteScalarAsync(this DbCommand @this, string commandText) =>
            await @this.SetCommandText(commandText).ExecuteScalarAsync().NoMarshalling();

        public static int ExecuteNonQuery(this DbCommand @this, string commandText) =>
            @this.SetCommandText(commandText).ExecuteNonQuery();

        public static async Task<int> ExecuteNonQueryAsync(this DbCommand @this, string commandText) =>
            await @this.SetCommandText(commandText).ExecuteNonQueryAsync().NoMarshalling();

        public static TCommand AppendCommandText<TCommand>(this TCommand @this, string append) where TCommand : DbCommand =>
            @this.Mutate(me => me.CommandText += append);

        public static TCommand SetCommandText<TCommand>(this TCommand @this, string commandText) where TCommand : DbCommand =>
            @this.Mutate(me => me.CommandText = commandText);

        public static TCommand SetStoredProcedure<TCommand>(this TCommand @this, string storedProcedure) where TCommand : DbCommand =>
            @this.Mutate(me =>
            {
                me.CommandType = CommandType.StoredProcedure;
                me.CommandText = storedProcedure;
            });

        public static IReadOnlyList<T> ExecuteReaderAndSelect<T, TCommand, TReader>(this TCommand @this, Func<TReader, T> select)
            where TCommand : DbCommand
            where TReader : DbDataReader
        {
            using var reader = (TReader)@this.ExecuteReader();
            var result = new List<T>();
            reader.ForEachSuccessfulRead(row => result.Add(select(row)));
            return result;
        }

        static readonly IReadOnlyList<string> ParameterPrefixes = EnumerableCE.Create(@"@", @":").ToArray();
        public static TCommand LogCommand<TCommand>(this TCommand @this) where TCommand : DbCommand
        {
            SafeConsole.WriteLine("####################################### Logging command###############################################");
            SafeConsole.WriteLine($@"{nameof(@this.CommandText)}:
{@this.CommandText}");

            var parameters = @this.Parameters.Cast<DbParameter>().ToList();

            if(parameters.Any())
            {
                parameters.ForEach(
                    parameter => Console.WriteLine($@"
    {nameof(parameter.ParameterName)}: {parameter.ParameterName}, 
{nameof(parameter.DbType)}: {parameter.DbType},
{nameof(parameter.Value)}: {parameter.Value},
{nameof(parameter.Size)}: {parameter.Size},
{nameof(parameter.Precision)}: {parameter.Precision},
{nameof(parameter.Direction)}: {parameter.Direction},
{nameof(parameter.IsNullable)}: {parameter.IsNullable}".ReplaceInvariant(Environment.NewLine, "")));

                SafeConsole.WriteLine("####################################### Hacking values into parameter positions #######################################");
                var commandTextWithParameterValues = @this.CommandText;
                parameters.ForEach(
                    parameter => ParameterPrefixes.ForEach(
                        prefix => commandTextWithParameterValues = commandTextWithParameterValues.ReplaceInvariant($"{prefix}{parameter.ParameterName}", parameter.Value == DBNull.Value ? "NULL" : parameter.Value?.ToString() ?? "NULL")));
                Console.WriteLine(commandTextWithParameterValues);
                SafeConsole.WriteLine("######################################################################################################");
            }

            return @this;
        }
    }

    static class DbDataReaderCE
    {
        //Urgent: In all persistence layers, replace all manual implementation of this.
        public static Guid GetGuidFromString(this DbDataReader @this, int index) => Guid.Parse(@this.GetString(index));
        public static void ForEachSuccessfulRead<TReader>(this TReader @this, Action<TReader> forEach) where TReader : DbDataReader
        {
            while(@this.Read()) forEach(@this);
        }
    }
}
