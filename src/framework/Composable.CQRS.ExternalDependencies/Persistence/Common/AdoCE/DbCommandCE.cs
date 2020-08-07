using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Logging;
using Composable.SystemCE;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.Persistence.Common.AdoCE
{
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


        public static object PrepareAndExecuteScalar(this DbCommand @this, string commandText) =>
            @this.SetCommandText(commandText).PrepareStatement().ExecuteScalar();

        public static async Task<object> PrepareAndExecuteScalarAsync(this DbCommand @this, string commandText) =>
            await @this.SetCommandText(commandText).PrepareStatement().ExecuteScalarAsync().NoMarshalling();

        public static int PrepareAndExecuteNonQuery(this DbCommand @this, string commandText) =>
            @this.SetCommandText(commandText).PrepareStatement().ExecuteNonQuery();

        public static async Task<int> PrepareAndExecuteNonQueryAsync(this DbCommand @this, string commandText) =>
            await @this.SetCommandText(commandText).PrepareStatement().ExecuteNonQueryAsync().NoMarshalling();

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

        public static TCommand PrepareStatement<TCommand>(this TCommand @this) where TCommand : DbCommand
        {
            Contract.Arguments.Assert(@this.CommandText.Length > 0, "Cannot prepare statement with empty CommandText");
            return @this.Mutate(me => me.Prepare());
        }

        public static async Task<TCommand> PrepareStatementAsync<TCommand>(this TCommand @this) where TCommand : DbCommand
        {
            Contract.Arguments.Assert(@this.CommandText.Length > 0, "Cannot prepare statement with empty CommandText");
            return await @this.MutateAsync(async me => await me.PrepareAsync().NoMarshalling()).NoMarshalling();
        }

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
            ConsoleCE.WriteLine("####################################### Logging command###############################################");
            ConsoleCE.WriteLine($@"{nameof(@this.CommandText)}:
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

                ConsoleCE.WriteLine("####################################### Hacking values into parameter positions #######################################");
                var commandTextWithParameterValues = @this.CommandText;
                parameters.ForEach(
                    parameter => ParameterPrefixes.ForEach(
                        prefix => commandTextWithParameterValues = commandTextWithParameterValues.ReplaceInvariant($"{prefix}{parameter.ParameterName}", parameter.Value == DBNull.Value ? "NULL" : parameter.Value?.ToString() ?? "NULL")));
                Console.WriteLine(commandTextWithParameterValues);
                ConsoleCE.WriteLine("######################################################################################################");
            }

            return @this;
        }
    }
}
