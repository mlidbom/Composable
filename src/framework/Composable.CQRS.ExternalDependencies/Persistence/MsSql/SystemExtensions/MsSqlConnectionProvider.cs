using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Transactions;
using Composable.System;
using Composable.System.Threading;

namespace Composable.Persistence.MsSql.SystemExtensions
{
    class MsSqlConnectionProvider : IMsSqlConnectionProvider
    {
        readonly OptimizedLazy<string> _connectionString;
        string GetConnectionString() => _connectionString.Value;

        public MsSqlConnectionProvider(string connectionString) : this(() => connectionString)
        {}

        public MsSqlConnectionProvider(Func<string> connectionString) => _connectionString = new OptimizedLazy<string>(connectionString);

        SqlConnection OpenConnection()
        {
            var transactionInformationDistributedIdentifierBefore = Transaction.Current?.TransactionInformation.DistributedIdentifier;
            var connectionString = GetConnectionString();
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
            return await func(connection).NoMarshalling();
        }


        public async Task UseConnectionAsync(Func<SqlConnection, Task> action)
        {
            await using var connection = OpenConnection();
            await action(connection).NoMarshalling();
        }
    }
}
