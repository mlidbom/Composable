using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Composable.System.Linq;

namespace Composable.Persistence.SqlServer.SystemExtensions
{
    static class SqlServerCommandExtensions
    {
        public static void ExecuteReader(this SqlCommand @this, string commandText, Action<SqlDataReader> forEach) => @this.ExecuteReader(commandText).ForEachSuccessfulRead(forEach);
        public static SqlDataReader ExecuteReader(this SqlCommand @this, string commandText) => @this.SetCommandText(commandText).ExecuteReader();
        public static object ExecuteScalar(this SqlCommand @this, string commandText) => @this.SetCommandText(commandText).ExecuteScalar();
        public static async Task<object> ExecuteScalarAsync(this SqlCommand @this, string commandText) => await @this.SetCommandText(commandText).ExecuteScalarAsync();
        public static void ExecuteNonQuery(this SqlCommand @this, string commandText) => @this.SetCommandText(commandText).ExecuteNonQuery();
        public static async Task ExecuteNonQueryAsync(this SqlCommand @this, string commandText) => await @this.SetCommandText(commandText).ExecuteNonQueryAsync();
        public static SqlCommand AppendCommandText(this SqlCommand @this, string append) => @this.Mutate(me => me.CommandText += append);
        public static SqlCommand SetCommandText(this SqlCommand @this, string commandText) => @this.Mutate(me => me.CommandText = commandText);
    }
}
