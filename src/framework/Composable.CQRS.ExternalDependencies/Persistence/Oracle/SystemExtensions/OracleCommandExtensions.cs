using System;
using System.Collections.Generic;
using System.Linq;
using Oracle.ManagedDataAccess.Client;
using System.Threading.Tasks;
using Composable.Logging;
using Composable.System.Linq;
using Composable.System.Reflection;

namespace Composable.Persistence.Oracle.SystemExtensions
{
    static class OracleCommandExtensions
    {
        public static void ExecuteReader(this OracleCommand @this, string commandText, Action<OracleDataReader> forEach) => @this.ExecuteReader(commandText).ForEachSuccessfulRead(forEach);
        public static OracleDataReader ExecuteReader(this OracleCommand @this, string commandText) => @this.SetCommandText(commandText).ExecuteReader();
        public static object ExecuteScalar(this OracleCommand @this, string commandText) => @this.SetCommandText(commandText).ExecuteScalar();
        public static async Task<object> ExecuteScalarAsync(this OracleCommand @this, string commandText) => await @this.SetCommandText(commandText).ExecuteScalarAsync();
        public static void ExecuteNonQuery(this OracleCommand @this, string commandText) => @this.SetCommandText(commandText).ExecuteNonQuery();
        public static async Task<int> ExecuteNonQueryAsync(this OracleCommand @this, string commandText) => await @this.SetCommandText(commandText).ExecuteNonQueryAsync();
        public static OracleCommand AppendCommandText(this OracleCommand @this, string append) => @this.Mutate(me => me.CommandText += append);
        public static OracleCommand SetCommandText(this OracleCommand @this, string commandText) => @this.Mutate(me => me.CommandText = commandText);

        //urgent: Create a version of this for the other persistence layers. It's crazy helpful.
        public static OracleCommand LogCommand(this OracleCommand @this)
        {
            SafeConsole.WriteLine($"####################################### Logging command###############################################");
            SafeConsole.WriteLine($@"{nameof(@this.CommandText)}:
{@this.CommandText}");

            var parameters = @this.Parameters.Cast<OracleParameter>().ToList();

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
{nameof(parameter.IsNullable)}: {parameter.IsNullable}".Replace(Environment.NewLine, "")));

                SafeConsole.WriteLine("####################################### Hacking values into parameter positions #######################################");
                var commandTextWithParameterValues = @this.CommandText;
                parameters.ForEach(parameter => commandTextWithParameterValues = commandTextWithParameterValues.Replace($":{parameter.ParameterName}", parameter.Value == DBNull.Value ? "NULL" : parameter.Value?.ToString()));
                Console.WriteLine(commandTextWithParameterValues);
                SafeConsole.WriteLine("######################################################################################################");
            }

            return @this;
        }


        public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this OracleCommand @this, Func<OracleDataReader, T> select)
        {
            using(var reader = @this.ExecuteReader())
            {
                var result = new List<T>();
                reader.ForEachSuccessfulRead(row => result.Add(select(row)));
                return result;
            }
        }
    }
}
