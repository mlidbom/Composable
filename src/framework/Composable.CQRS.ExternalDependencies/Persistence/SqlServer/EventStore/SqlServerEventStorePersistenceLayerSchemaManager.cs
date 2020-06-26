using System;
using System.Data.SqlClient;
using System.Transactions;
using Composable.Logging;
using Composable.Persistence.EventStore;
using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.System.Transactions;

namespace Composable.Persistence.SqlServer.EventStore
{
    class SqlServerEventStorePersistenceLayerSchemaManager : IEventStorePersistenceLayer.ISchemaManager
    {
        bool _verifiedConnectionString;
        readonly SqlServerEventTableSchemaManager _eventTable = new SqlServerEventTableSchemaManager();
        public SqlServerEventStorePersistenceLayerSchemaManager(ISqlServerConnectionProvider sqlConnectionProvider)
            => _connectionProvider = sqlConnectionProvider;

        readonly ISqlServerConnectionProvider _connectionProvider;

        SqlConnection OpenConnection()
        {
            if (Transaction.Current == null)
            {
                this.Log().Warning($@"No ambient transaction. This is dangerous:
AT: 

{Environment.StackTrace}");
            }
            return _connectionProvider.OpenConnection();
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
