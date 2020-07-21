using System;
using System.Collections.Generic;
using Npgsql;
using System.Threading.Tasks;
using Composable.System.Linq;
using Composable.System.Threading;

namespace Composable.Persistence.PgSql.SystemExtensions
{
    static class MyNpgsqlCommandExtensions
    {
        public static void ExecuteReader(this NpgsqlCommand @this, string commandText, Action<NpgsqlDataReader> forEach) => @this.ExecuteReader(commandText).ForEachSuccessfulRead(forEach);
        public static NpgsqlDataReader ExecuteReader(this NpgsqlCommand @this, string commandText) => @this.SetCommandText(commandText).ExecuteReader();
        public static object ExecuteScalar(this NpgsqlCommand @this, string commandText) => @this.SetCommandText(commandText).ExecuteScalar();
        public static async Task<object> ExecuteScalarAsync(this NpgsqlCommand @this, string commandText) => await @this.SetCommandText(commandText).ExecuteScalarAsync().NoMarshalling();
        public static void ExecuteNonQuery(this NpgsqlCommand @this, string commandText) => @this.SetCommandText(commandText).ExecuteNonQuery();
        public static async Task<int> ExecuteNonQueryAsync(this NpgsqlCommand @this, string commandText) => await @this.SetCommandText(commandText).ExecuteNonQueryAsync().NoMarshalling();
        public static NpgsqlCommand AppendCommandText(this NpgsqlCommand @this, string append) => @this.Mutate(me => me.CommandText += append);
        public static NpgsqlCommand SetCommandText(this NpgsqlCommand @this, string commandText) => @this.Mutate(me => me.CommandText = commandText);
        public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this NpgsqlCommand @this, Func<NpgsqlDataReader, T> select)
        {
            using var reader = @this.ExecuteReader();
            var result = new List<T>();
            reader.ForEachSuccessfulRead(row => result.Add(select(row)));
            return result;
        }
    }
}
