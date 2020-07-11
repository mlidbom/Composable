using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using System.Threading.Tasks;
using Composable.System.Linq;

namespace Composable.Persistence.Oracle.SystemExtensions
{
    static class MyOracleCommandExtensions
    {
        public static void ExecuteReader(this OracleCommand @this, string commandText, Action<OracleDataReader> forEach) => @this.ExecuteReader(commandText).ForEachSuccessfulRead(forEach);
        public static OracleDataReader ExecuteReader(this OracleCommand @this, string commandText) => @this.SetCommandText(commandText).ExecuteReader();
        public static object ExecuteScalar(this OracleCommand @this, string commandText) => @this.SetCommandText(commandText).ExecuteScalar();
        public static async Task<object> ExecuteScalarAsync(this OracleCommand @this, string commandText) => await @this.SetCommandText(commandText).ExecuteScalarAsync();
        public static void ExecuteNonQuery(this OracleCommand @this, string commandText) => @this.SetCommandText(commandText).ExecuteNonQuery();
        public static async Task<int> ExecuteNonQueryAsync(this OracleCommand @this, string commandText) => await @this.SetCommandText(commandText).ExecuteNonQueryAsync();
        public static OracleCommand AppendCommandText(this OracleCommand @this, string append) => @this.Mutate(me => me.CommandText += append);
        public static OracleCommand SetCommandText(this OracleCommand @this, string commandText) => @this.Mutate(me => me.CommandText = commandText);
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
