using System;
using System.Data;
using System.Data.SqlClient;
using System.Transactions;
using Composable.System.Linq;

namespace Composable.System.Data.SqlClient
{
    class SqlServerConnection : ISqlConnection
    {
        public string ConnectionString { get; }

        public SqlServerConnection(string connectionString) => ConnectionString = connectionString;

        public SqlConnection OpenConnection()
        {
            var transactionInformationDistributedIdentifierBefore = Transaction.Current?.TransactionInformation.DistributedIdentifier;
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            if(transactionInformationDistributedIdentifierBefore != null && transactionInformationDistributedIdentifierBefore.Value == Guid.Empty)
            {
                if(Transaction.Current.TransactionInformation.DistributedIdentifier != Guid.Empty)
                {
                    throw new Exception("Opening connection escalated transaction to distributed. For now this is disallowed");
                }
            }

            return connection;
        }
    }

    static class SqlConnectionExtensions
    {
        public static void UseCommand(this SqlConnection @this, Action<SqlCommand> action)
        {
            using(var command = @this.CreateCommand())
            {
                action(command);
            }
        }

        public static TResult UseCommand<TResult>(this SqlConnection @this, Func<SqlCommand, TResult> action)
        {
            using(var command = @this.CreateCommand())
            {
                return action(command);
            }
        }

        public static void ExecuteNonQuery(this SqlConnection @this, string commandText) => @this.UseCommand(command => command.ExecuteNonQuery(commandText));
        public static object ExecuteScalar(this SqlConnection @this, string commandText) => @this.UseCommand(command => command.ExecuteScalar(commandText));
        public static void ExecuteReader(this SqlConnection @this, string commandText, Action<SqlDataReader> forEach) => @this.UseCommand(command => command.ExecuteReader(commandText, forEach));
    }

    static class SqlDataReaderExtensions
    {
        public static void ForEachSuccessfulRead(this SqlDataReader @this, Action<SqlDataReader> forEach)
        {
            while(@this.Read()) forEach(@this);
        }
    }

    static class SqlConnectionProviderExtensions
    {
        public static int ExecuteNonQuery(this ISqlConnection @this, string commandText) => @this.UseCommand(command => command.SetCommandText(commandText).ExecuteNonQuery());

        public static object ExecuteScalar(this ISqlConnection @this, string commandText) => @this.UseCommand(command => command.SetCommandText(commandText).ExecuteScalar());

        public static void ExecuteReader(this ISqlConnection @this, string commandText, Action<SqlDataReader> forEach) => @this.UseCommand(command => command.ExecuteReader(commandText, forEach));

        public static void UseConnection(this ISqlConnection @this, Action<SqlConnection> action)
        {
            using(var connection = @this.OpenConnection())
            {
                action(connection);
            }
        }

        static TResult UseConnection<TResult>(this ISqlConnection @this, Func<SqlConnection, TResult> action)
        {
            using(var connection = @this.OpenConnection())
            {
                return action(connection);
            }
        }

        public static void UseCommand(this ISqlConnection @this, Action<SqlCommand> action) => @this.UseConnection(connection => connection.UseCommand(action));

        public static TResult UseCommand<TResult>(this ISqlConnection @this, Func<SqlCommand, TResult> action) => @this.UseConnection(connection => connection.UseCommand(action));
    }

    static class SqlCommandExtensions
    {
        public static void ExecuteReader(this SqlCommand @this, string commandText, Action<SqlDataReader> forEach) => @this.ExecuteReader(commandText).ForEachSuccessfulRead(forEach);
        public static SqlDataReader ExecuteReader(this SqlCommand @this, string commandText) => @this.SetCommandText(commandText).ExecuteReader();
        public static object ExecuteScalar(this SqlCommand @this, string commandText) => @this.SetCommandText(commandText).ExecuteScalar();
        public static void ExecuteNonQuery(this SqlCommand @this, string commandText) => @this.SetCommandText(commandText).ExecuteNonQuery();
        public static SqlCommand AppendCommandText(this SqlCommand @this, string append) => @this.Mutate(me => me.CommandText = me.CommandText + append);
        public static SqlCommand SetCommandText(this SqlCommand @this, string commandText) => @this.Mutate(me => me.CommandText = commandText);
    }

    static class SqlCommandParameterExtensions
    {
        public static SqlCommand AddParameter(this SqlCommand @this, string name, int value) => AddParameter(@this, name, SqlDbType.Int, value);
        public static SqlCommand AddParameter(this SqlCommand @this, string name, Guid value) => AddParameter(@this, name, SqlDbType.UniqueIdentifier, value);
        public static SqlCommand AddNVarcharParameter(this SqlCommand @this, string name, int length, string value) => AddParameter(@this, name, SqlDbType.NVarChar, value, length);
        public static SqlCommand AddNVarcharMaxParameter(this SqlCommand @this, string name, string value) => AddParameter(@this, name, SqlDbType.NVarChar, value, -1);

        public static SqlCommand AddParameter(this SqlCommand @this, SqlParameter parameter)
        {
            @this.Parameters.Add(parameter);
            return @this;
        }

        static SqlCommand AddParameter(SqlCommand @this, string name, SqlDbType type, object value, int length) => @this.AddParameter(new SqlParameter(name, type, length) {Value = value});

        static SqlCommand AddParameter(SqlCommand @this, string name, SqlDbType type, object value) => @this.AddParameter(new SqlParameter(name, type) {Value = value});
    }

    interface ISqlConnection
    {
        SqlConnection OpenConnection();
        string ConnectionString { get; }
    }
}
