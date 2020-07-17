using System;
using System.Threading.Tasks;
using IBM.Data.DB2.Core;
using System.Transactions;
using Composable.Logging;
using Composable.System;
using Composable.System.Diagnostics;
using Composable.System.Threading;

namespace Composable.Persistence.DB2.SystemExtensions
{
    class DB2ConnectionProvider : IDB2ConnectionProvider
    {
        string ConnectionString { get; }
        public DB2ConnectionProvider(string connectionString) => ConnectionString = connectionString;

        DB2Connection OpenConnection()
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

        //Urgent: Since the DB2 connection pooling is way slow we should do something about that here. Something like using Task to keep a pool of open connections on hand.
        DB2Connection GetConnectionFromPool()
        {
            var connection = new DB2Connection(ConnectionString);
            connection.Open();
            return connection;
        }

        public TResult UseConnection<TResult>(Func<DB2Connection, TResult> func)
        {
            using var connection = OpenConnection();
            return func(connection);
        }

        public void UseConnection(Action<DB2Connection> action) => UseConnection(connection =>
        {
            action(connection);
            return 1;
        });

        //Urgent: All variations, in all persistence layers, on these async methods should use OpenAsync method on the connection.
        public async Task<TResult> UseConnectionAsync<TResult>(Func<DB2Connection, Task<TResult>> func)
        {
            await using var connection = OpenConnection();
            return await func(connection).NoMarshalling();
        }


        public async Task UseConnectionAsync(Func<DB2Connection, Task> action)
        {
            await using var connection = OpenConnection();
            await action(connection).NoMarshalling();
        }
    }
}
