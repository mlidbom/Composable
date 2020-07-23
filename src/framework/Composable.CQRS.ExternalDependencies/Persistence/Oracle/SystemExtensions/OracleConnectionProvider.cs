using System;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using System.Transactions;
using Composable.Logging;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Persistence.Oracle.SystemExtensions
{
    class OracleConnectionProvider : IOracleConnectionProvider
    {
        readonly OptimizedLazy<string> _connectionString;
        string GetConnectionString() => _connectionString.Value;

        public OracleConnectionProvider(string connectionString) : this(() => connectionString)
        {}

        public OracleConnectionProvider(Func<string> connectionString) => _connectionString = new OptimizedLazy<string>(connectionString);

        OracleConnection OpenConnection()
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

        //Urgent: Since the Oracle connection pooling is way slow we should do something about that here. Something like using Task to keep a pool of open connections on hand.
        OracleConnection GetConnectionFromPool()
        {
            var connectionString = GetConnectionString();
            var connection = new OracleConnection(connectionString);
            connection.Open();
            return connection;
        }

        public TResult UseConnection<TResult>(Func<OracleConnection, TResult> func)
        {
            using var connection = OpenConnection();
            return func(connection);
        }

        public void UseConnection(Action<OracleConnection> action) => UseConnection(connection =>
        {
            action(connection);
            return 1;
        });

        //Urgent: All variations, in all persistence layers, on these async methods should use OpenAsync method on the connection.
        public async Task<TResult> UseConnectionAsync<TResult>(Func<OracleConnection, Task<TResult>> func)
        {
            await using var connection = OpenConnection();
            return await func(connection).NoMarshalling();
        }


        public async Task UseConnectionAsync(Func<OracleConnection, Task> action)
        {
            await using var connection = OpenConnection();
            await action(connection).NoMarshalling();
        }
    }
}
