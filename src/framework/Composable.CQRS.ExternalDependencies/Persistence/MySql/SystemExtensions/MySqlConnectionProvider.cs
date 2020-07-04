using System;
using MySql.Data.MySqlClient;
using System.Transactions;

namespace Composable.Persistence.MySql.SystemExtensions
{
    class MySqlConnectionProvider : IMySqlConnectionProvider
    {
        public string ConnectionString { get; }
        public MySqlConnectionProvider(string connectionString) => ConnectionString = connectionString;

        public MySqlConnection OpenConnection()
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
    }
}
