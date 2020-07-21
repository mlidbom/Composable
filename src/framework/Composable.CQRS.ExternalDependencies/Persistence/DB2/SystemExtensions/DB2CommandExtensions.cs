using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using IBM.Data.DB2.Core;
using System.Threading.Tasks;
using Composable.Logging;
using Composable.System;
using Composable.System.Linq;
using Composable.System.Reflection;
using Composable.System.Threading;

namespace Composable.Persistence.DB2.SystemExtensions
{
    static class DB2CommandExtensions
    {
        public static void ExecuteReader(this DB2Command @this, string commandText, Action<DB2DataReader> forEach) => @this.ExecuteReader(commandText).ForEachSuccessfulRead(forEach);
        public static DB2DataReader ExecuteReader(this DB2Command @this, string commandText) => @this.SetCommandText(commandText).ExecuteReader();
        public static object ExecuteScalar(this DB2Command @this, string commandText) => @this.SetCommandText(commandText).ExecuteScalar();
        public static async Task<object> ExecuteScalarAsync(this DB2Command @this, string commandText) => await @this.SetCommandText(commandText).ExecuteScalarAsync().NoMarshalling();
        public static void ExecuteNonQuery(this DB2Command @this, string commandText) => @this.SetCommandText(commandText).ExecuteNonQuery();
        public static async Task<int> ExecuteNonQueryAsync(this DB2Command @this, string commandText) => await @this.SetCommandText(commandText).ExecuteNonQueryAsync().NoMarshalling();
        public static DB2Command AppendCommandText(this DB2Command @this, string append) => @this.Mutate(me => me.CommandText += append);
        public static DB2Command SetCommandText(this DB2Command @this, string commandText) => @this.Mutate(me => me.CommandText = commandText);
        public static DB2Command SetStoredProcedure(this DB2Command @this, string storedProcedure) => @this.Mutate(me =>
        {
            me.CommandType = CommandType.StoredProcedure;
            me.CommandText = storedProcedure;
        });

        //urgent: Create a version of this for the other persistence layers. It's crazy helpful.
        public static DB2Command LogCommand(this DB2Command @this)
        {
            SafeConsole.WriteLine("####################################### Logging command###############################################");
            SafeConsole.WriteLine($@"{nameof(@this.CommandText)}:
{@this.CommandText}");

            var parameters = @this.Parameters.Cast<DB2Parameter>().ToList();

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
                parameters.ForEach(parameter => commandTextWithParameterValues = commandTextWithParameterValues.ReplaceInvariant($"@{parameter.ParameterName}", parameter.Value == DBNull.Value ? "NULL" : parameter.Value?.ToString() ?? "NULL"));
                Console.WriteLine(commandTextWithParameterValues);
                SafeConsole.WriteLine("######################################################################################################");
            }

            return @this;
        }


        public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this DB2Command @this, Func<DB2DataReader, T> select)
        {
            using var reader = @this.ExecuteReader();
            var result = new List<T>();
            reader.ForEachSuccessfulRead(row => result.Add(select(row)));
            return result;
        }
    }
}
