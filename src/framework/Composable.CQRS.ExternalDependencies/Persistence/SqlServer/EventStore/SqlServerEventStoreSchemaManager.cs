using System;
using System.Data.SqlClient;
using System.Transactions;
using Composable.Logging;
using Composable.Persistence.EventStore;
using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.Refactoring.Naming;
using Composable.System.Transactions;

namespace Composable.Persistence.SqlServer.EventStore
{
    class SqlServerEventStoreSchemaManager : IEventStoreSchemaManager
    {
        bool _verifiedConnectionString;
        readonly SqlServerEventTableSchemaManager _eventTable = new SqlServerEventTableSchemaManager();
        readonly SqlServerEventTypeTableSchemaManager _eventTypeTable = new SqlServerEventTypeTableSchemaManager();
        public SqlServerEventStoreSchemaManager(ISqlConnectionProvider connectionString, ITypeMapper typeMapper)
        {
            _connectionProvider = connectionString;
            IdMapper = new SqlServerEventStoreEventTypeToIdMapper(_connectionProvider, typeMapper);
        }

        public IEventTypeToIdMapper IdMapper { get; private set; }

        readonly ISqlConnectionProvider _connectionProvider;

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
    {_eventTypeTable.CreateTableSql}

    {_eventTable.CreateTableSql}
END 
");

                _verifiedConnectionString = true;
            }
        });
    }
}
