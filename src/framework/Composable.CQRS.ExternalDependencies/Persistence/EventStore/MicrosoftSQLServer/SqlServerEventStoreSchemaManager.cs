using System;
using System.Data.SqlClient;
using System.Transactions;
using Composable.Logging;
using Composable.Refactoring.Naming;
using Composable.System.Data.SqlClient;
using Composable.System.Transactions;

namespace Composable.Persistence.EventStore.MicrosoftSQLServer
{
    class SqlServerEventStoreSchemaManager : IEventStoreSchemaManager
    {
        bool _verifiedConnectionString;
        readonly EventTableSchemaManager _eventTable = new EventTableSchemaManager();
        readonly EventTypeTableSchemaManager _eventTypeTable = new EventTypeTableSchemaManager();
        public SqlServerEventStoreSchemaManager(ISqlConnectionProvider connectionString, ITypeMapper typeMapper)
        {
            _connectionProvider = connectionString;
            _typeMapper = typeMapper;
        }

        readonly ITypeMapper _typeMapper;

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
                using(var connection = OpenConnection())
                {
                    IdMapper = new SqlServerEventStoreEventTypeToIdMapper(_connectionProvider, _typeMapper);

                    connection.ExecuteNonQuery($@"
IF NOT EXISTS(SELECT NAME FROM sys.tables WHERE name = '{_eventTable.Name}')
BEGIN
    {_eventTypeTable.CreateTableSql}

    {_eventTable.CreateTableSql}
END 
");

                    _verifiedConnectionString = true;
                }
            }
        });
    }
}
