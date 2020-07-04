using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using System.Transactions;
using Composable.System;

namespace Composable.Persistence.MySql.SystemExtensions
{
    class MySqlConnectionProvider : IMySqlConnectionProvider
    {
        public string ConnectionString { get; }
        public MySqlConnectionProvider(string connectionString) => ConnectionString = connectionString;

        public MySqlConnection OpenConnection()
        {
            var transactionInformationDistributedIdentifierBefore = Transaction.Current?.TransactionInformation.DistributedIdentifier;
            var connectionString = ConnectionString;
            var connection = new MySqlConnection(connectionString);
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

    class LazyMySqlConnectionProvider : IMySqlConnectionProvider
    {
        readonly OptimizedLazy<IMySqlConnectionProvider> _connectionProvider;

        public LazyMySqlConnectionProvider(Func<string> connectionStringFactory) => _connectionProvider = new OptimizedLazy<IMySqlConnectionProvider>(() => new MySqlConnectionProvider(connectionStringFactory()));

        public MySqlConnection OpenConnection() => _connectionProvider.Value.OpenConnection();
        public string ConnectionString => _connectionProvider.Value.ConnectionString;
    }

    static class MySqlDataReaderExtensions
    {
        public static void ForEachSuccessfulRead(this MySqlDataReader @this, Action<MySqlDataReader> forEach)
        {
            while(@this.Read()) forEach(@this);
        }
    }

    static class MySqlCommandParameterExtensions
    {
        public static MySqlCommand AddParameter(this MySqlCommand @this, string name, int value) => AddParameter(@this, name, MySqlDbType.Int32, value);
        public static MySqlCommand AddParameter(this MySqlCommand @this, string name, Guid value) => AddParameter(@this, name, MySqlDbType.Guid, value);
        public static MySqlCommand AddDateTime2Parameter(this MySqlCommand @this, string name, DateTime value) => AddParameter(@this, name, MySqlDbType.DateTime, value);
        public static MySqlCommand AddVarcharParameter(this MySqlCommand @this, string name, int length, string value) => AddParameter(@this, name, MySqlDbType.VarString, value, length);

        public static MySqlCommand AddParameter(this MySqlCommand @this, MySqlParameter parameter)
        {
            @this.Parameters.Add(parameter);
            return @this;
        }

        static MySqlCommand AddParameter(MySqlCommand @this, string name, MySqlDbType type, object value, int length) => @this.AddParameter(new MySqlParameter(name, type, length) {Value = value});

        public static MySqlCommand AddParameter(this MySqlCommand @this, string name, MySqlDbType type, object value) => @this.AddParameter(new MySqlParameter(name, type) {Value = value});

        public static MySqlCommand AddNullableParameter(this MySqlCommand @this, string name, MySqlDbType type, object? value) => @this.AddParameter(Nullable(new MySqlParameter(name, type) {Value = value}));

        static MySqlParameter Nullable(MySqlParameter @this)
        {
            @this.IsNullable = true;
            @this.Direction = ParameterDirection.Input;
            if(@this.Value == null)
            {
                @this.Value = DBNull.Value;
            }
            return @this;
        }
    }

    interface IMySqlConnectionProvider
    {
        MySqlConnection OpenConnection();
        string ConnectionString { get; }
    }
}
