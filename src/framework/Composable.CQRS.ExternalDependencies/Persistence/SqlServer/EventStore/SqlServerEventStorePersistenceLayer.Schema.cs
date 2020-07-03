using System;
using System.Data.SqlClient;
using System.Transactions;
using Composable.Logging;
using Composable.Persistence.EventStore;
using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.System.Transactions;

namespace Composable.Persistence.SqlServer.EventStore
{
    partial class SqlServerEventStorePersistenceLayer : IEventStorePersistenceLayer
    {
        bool _verifiedConnectionString;
        readonly SqlServerEventTableSchemaManager _eventTable = new SqlServerEventTableSchemaManager();

        SqlConnection OpenConnection()
        {
            if (Transaction.Current == null)
            {
                this.Log().Warning($@"No ambient transaction. This is dangerous:
AT: 

{Environment.StackTrace}");
            }
            return _connectionManager.OpenConnection();
        }

        public void SetupSchemaIfDatabaseUnInitialized() => TransactionScopeCe.SuppressAmbientAndExecuteInNewTransaction(() =>
        {
            if(!_verifiedConnectionString)
            {
                using var connection = OpenConnection();

                connection.ExecuteNonQuery($@"
IF NOT EXISTS(SELECT NAME FROM sys.tables WHERE name = '{_eventTable.Name}')
BEGIN
    {_eventTable.CreateTableSql}
END 
");

                _verifiedConnectionString = true;
            }
        });
    }
}
