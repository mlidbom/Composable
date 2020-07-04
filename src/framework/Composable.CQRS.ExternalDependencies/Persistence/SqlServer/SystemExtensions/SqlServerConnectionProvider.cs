using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Transactions;

namespace Composable.Persistence.SqlServer.SystemExtensions
{
    class SqlServerConnectionProvider : ISqlServerConnectionProvider
    {
        string ConnectionString { get; }
        public SqlServerConnectionProvider(string connectionString) => ConnectionString = connectionString;

        SqlConnection OpenConnection()
        {
            var transactionInformationDistributedIdentifierBefore = Transaction.Current?.TransactionInformation.DistributedIdentifier;
            var connectionString = ConnectionString;
            var connection = new SqlConnection(connectionString);
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

        public TResult UseConnection<TResult>(Func<SqlConnection, TResult> func)
        {
            using var connection = OpenConnection();
            return func(connection);
        }

        public void UseConnection(Action<SqlConnection> action) => UseConnection(connection =>
        {
            action(connection);
            return 1;
        });

        public async Task<TResult> UseConnectionAsync<TResult>(Func<SqlConnection, Task<TResult>> func)
        {
            await using var connection = OpenConnection();
            return await func(connection);
        }


        public async Task UseConnectionAsync(Func<SqlConnection, Task> action)
        {
            await using var connection = OpenConnection();
            await action(connection);
        }
    }
}
