using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using System.Transactions;
using Composable.SystemCE;
using Composable.SystemCE.Collections;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.SystemCE.TransactionsCE;

namespace Composable.Persistence.PgSql.SystemExtensions
{
    class PgSqlConnectionProvider : INpgsqlConnectionProvider
    {
        readonly OptimizedLazy<string> _connectionString;
        string GetConnectionString() => _connectionString.Value;

        public PgSqlConnectionProvider(string connectionString) : this(() => connectionString)
        {}

        public PgSqlConnectionProvider(Func<string> connectionString) => _connectionString = new OptimizedLazy<string>(connectionString);



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

        readonly OptimizedThreadShared<Dictionary<string, NpgsqlConnection>> _transactionConnections = new OptimizedThreadShared<Dictionary<string, NpgsqlConnection>>(new Dictionary<string, NpgsqlConnection>());
        NpgsqlConnection GetConnectionFromPool()
        {
            var connectionString = GetConnectionString();
            var connection = new NpgsqlConnection(connectionString);
                connection.Open();
                return connection;
         }

        public TResult UseConnection<TResult>(Func<NpgsqlConnection, TResult> func)
        {
            if(Transaction.Current == null)
            {
                using var connection = OpenConnection();
                return func(connection);
            } else
            {
                //PostgreSql has two problems with opening multiple connection within the same transaction: 1: It causes the transaction to escalate to distributed. 2: The other connections are unable to read data inserted by the first connection causing all sorts of havoc.
                var connection = _transactionConnections.WithExclusiveAccess(@this => @this.GetOrAdd(Transaction.Current.TransactionInformation.LocalIdentifier,
                                                                                               () =>
                                                                                               {
                                                                                                   var transactionId = Transaction.Current.TransactionInformation.LocalIdentifier;
                                                                                                   var createdConnection = OpenConnection();
                                                                                                   Transaction.Current.OnCompleted(() => _transactionConnections.WithExclusiveAccess(me =>
                                                                                                   {
                                                                                                       createdConnection.Dispose();
                                                                                                       me.Remove(transactionId);
                                                                                                   }));
                                                                                                   return createdConnection;
                                                                                               }));
                return func(connection);
            }
        }

        public void UseConnection(Action<NpgsqlConnection> action) => UseConnection(connection =>
        {
            action(connection);
            return 1;
        });

        public async Task<TResult> UseConnectionAsync<TResult>(Func<NpgsqlConnection, Task<TResult>> func)
        {
            await using var connection = OpenConnection();
            return await func(connection).NoMarshalling();
        }


        public async Task UseConnectionAsync(Func<NpgsqlConnection, Task> action)
        {
            await using var connection = OpenConnection();
            await action(connection).NoMarshalling();
        }

    }
}
