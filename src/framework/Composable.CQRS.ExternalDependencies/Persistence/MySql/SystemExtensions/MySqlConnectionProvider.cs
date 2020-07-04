using System;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Transactions;

namespace Composable.Persistence.MySql.SystemExtensions
{
    class MySqlConnectionProvider : IMySqlConnectionProvider
    {
        string ConnectionString { get; }
        public MySqlConnectionProvider(string connectionString) => ConnectionString = connectionString;

        MySqlConnection OpenConnection()
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

        //Urgent: Since the MySql connection pooling is way slow we should do something about that here. Something like using Task to keep a pool of open connections on hand.
        MySqlConnection GetConnectionFromPool()
        {
            var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            return connection;
        }

        public TResult UseConnection<TResult>(Func<MySqlConnection, TResult> func)
        {
            using var connection = OpenConnection();
            return func(connection);
        }

        public void UseConnection(Action<MySqlConnection> action) => UseConnection(connection =>
        {
            action(connection);
            return 1;
        });

        public async Task<TResult> UseConnectionAsync<TResult>(Func<MySqlConnection, Task<TResult>> func)
        {
            await using var connection = OpenConnection();
            return await func(connection);
        }


        public async Task UseConnectionAsync(Func<MySqlConnection, Task> action)
        {
            await using var connection = OpenConnection();
            await action(connection);
        }
    }
}
