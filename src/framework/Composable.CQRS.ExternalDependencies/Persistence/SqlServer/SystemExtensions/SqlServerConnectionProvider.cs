using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Transactions;
using Composable.System;

namespace Composable.Persistence.SqlServer.SystemExtensions
{
    class SqlServerConnectionProvider : LazySqlServerConnectionProvider
    {
        public SqlServerConnectionProvider(string connectionString) : base(() => connectionString) {}
    }

    class LazySqlServerConnectionProvider : ISqlServerConnectionProvider
    {
        readonly OptimizedLazy<string> _connectionString;
        public string ConnectionString => _connectionString.Value;

        public LazySqlServerConnectionProvider(Func<string> connectionStringFactory) => _connectionString = new OptimizedLazy<string>(connectionStringFactory);

        public SqlConnection OpenConnection()
        {
            var transactionInformationDistributedIdentifierBefore = Transaction.Current?.TransactionInformation.DistributedIdentifier;
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            if(transactionInformationDistributedIdentifierBefore != null && transactionInformationDistributedIdentifierBefore.Value == Guid.Empty)
            {
                if(Transaction.Current!.TransactionInformation.DistributedIdentifier != Guid.Empty)
                {
                    throw new Exception("Opening connection escalated transaction to distributed. For now this is disallowed");
                }
            }

            return connection;
        }
    }

    static class SqlDataReaderExtensions
    {
        public static void ForEachSuccessfulRead(this SqlDataReader @this, Action<SqlDataReader> forEach)
        {
            while(@this.Read()) forEach(@this);
        }
    }

    static class SqlCommandParameterExtensions
    {
        public static SqlCommand AddParameter(this SqlCommand @this, string name, int value) => AddParameter(@this, name, SqlDbType.Int, value);
        public static SqlCommand AddParameter(this SqlCommand @this, string name, Guid value) => AddParameter(@this, name, SqlDbType.UniqueIdentifier, value);
        public static SqlCommand AddDateTime2Parameter(this SqlCommand @this, string name, DateTime value) => AddParameter(@this, name, SqlDbType.DateTime2, value);
        public static SqlCommand AddNVarcharParameter(this SqlCommand @this, string name, int length, string value) => AddParameter(@this, name, SqlDbType.NVarChar, value, length);
        public static SqlCommand AddNVarcharMaxParameter(this SqlCommand @this, string name, string value) => AddParameter(@this, name, SqlDbType.NVarChar, value, -1);

        public static SqlCommand AddParameter(this SqlCommand @this, SqlParameter parameter)
        {
            @this.Parameters.Add(parameter);
            return @this;
        }

        static SqlCommand AddParameter(SqlCommand @this, string name, SqlDbType type, object value, int length) => @this.AddParameter(new SqlParameter(name, type, length) {Value = value});

        public static SqlCommand AddParameter(this SqlCommand @this, string name, SqlDbType type, object value) => @this.AddParameter(new SqlParameter(name, type) {Value = value});
    }

    interface ISqlServerConnectionProvider
    {
        SqlConnection OpenConnection();
        string ConnectionString { get; }
    }
}
