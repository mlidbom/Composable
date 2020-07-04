using System;
using System.Data.SqlClient;
using System.Transactions;

namespace Composable.Persistence.SqlServer.SystemExtensions
{
    class SqlServerConnectionProvider : ISqlServerConnectionProvider
    {
        public string ConnectionString { get; }
        public SqlServerConnectionProvider(string connectionString) => ConnectionString = connectionString;

        public SqlConnection OpenConnection()
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
    }
}
