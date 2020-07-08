using System;
using System.Threading.Tasks;
using Npgsql;
using System.Transactions;

namespace Composable.Persistence.PgSql.SystemExtensions
{
    class NpgsqlConnectionProvider : INpgsqlConnectionProvider
    {
        string ConnectionString { get; }
        public NpgsqlConnectionProvider(string connectionString) => ConnectionString = connectionString;

        NpgsqlConnection OpenConnection()
        {
            var transactionInformationDistributedIdentifierBefore = Transaction.Current?.TransactionInformation.DistributedIdentifier;
            var connection = GetConnectionFromPool();

            if(transactionInformationDistributedIdentifierBefore != null && transactionInformationDistributedIdentifierBefore.Value == Guid.Empty)
            {
                if(Transaction.Current!.TransactionInformation.DistributedIdentifier != Guid.Empty)
                {
                    throw new Exception("Opening connection escalated transaction to distributed. For now this is disallowed");
                }
            }
            return connection;
        }

        //Urgent: Since the PgSql connection pooling is way slow we should do something about that here. Something like using Task to keep a pool of open connections on hand.
        NpgsqlConnection GetConnectionFromPool()
        {
            var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();
            return connection;
        }

        public TResult UseConnection<TResult>(Func<NpgsqlConnection, TResult> func)
        {
            using var connection = OpenConnection();
            return func(connection);
        }

        public void UseConnection(Action<NpgsqlConnection> action) => UseConnection(connection =>
        {
            action(connection);
            return 1;
        });

        public async Task<TResult> UseConnectionAsync<TResult>(Func<NpgsqlConnection, Task<TResult>> func)
        {
            await using var connection = OpenConnection();
            return await func(connection);
        }


        public async Task UseConnectionAsync(Func<NpgsqlConnection, Task> action)
        {
            await using var connection = OpenConnection();
            await action(connection);
        }
    }
}
