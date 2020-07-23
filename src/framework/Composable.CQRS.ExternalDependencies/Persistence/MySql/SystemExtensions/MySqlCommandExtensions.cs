using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using Composable.SystemCE.Linq;
using Composable.SystemCE.Reflection.Threading;

namespace Composable.Persistence.MySql.SystemExtensions
{
    static class MyMySqlCommandExtensions
    {
        public static void ExecuteReader(this MySqlCommand @this, string commandText, Action<MySqlDataReader> forEach) => @this.ExecuteReader(commandText).ForEachSuccessfulRead(forEach);
        public static MySqlDataReader ExecuteReader(this MySqlCommand @this, string commandText) => @this.SetCommandText(commandText).ExecuteReader();
        public static object ExecuteScalar(this MySqlCommand @this, string commandText) => @this.SetCommandText(commandText).ExecuteScalar();
        public static async Task<object> ExecuteScalarAsync(this MySqlCommand @this, string commandText) => await @this.SetCommandText(commandText).ExecuteScalarAsync().NoMarshalling();
        public static void ExecuteNonQuery(this MySqlCommand @this, string commandText) => @this.SetCommandText(commandText).ExecuteNonQuery();
        public static async Task<int> ExecuteNonQueryAsync(this MySqlCommand @this, string commandText) => await @this.SetCommandText(commandText).ExecuteNonQueryAsync().NoMarshalling();
        public static MySqlCommand AppendCommandText(this MySqlCommand @this, string append) => @this.Mutate(me => me.CommandText += append);
        public static MySqlCommand SetCommandText(this MySqlCommand @this, string commandText) => @this.Mutate(me => me.CommandText = commandText);
        public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this MySqlCommand @this, Func<MySqlDataReader, T> select)
        {
            using var reader = @this.ExecuteReader();
            var result = new List<T>();
            reader.ForEachSuccessfulRead(row => result.Add(select(row)));
            return result;
        }
    }
}
