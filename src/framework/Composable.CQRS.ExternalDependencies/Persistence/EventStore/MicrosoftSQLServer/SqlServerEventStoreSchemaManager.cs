using System;
using System.Data.SqlClient;
using System.Transactions;
using Composable.Logging.Log4Net;
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
        public SqlServerEventStoreSchemaManager(ISqlConnection connectionString, ITypeIdMapper typeIdMapper)
        {
            _connectionManager = connectionString;
            _typeIdMapper = typeIdMapper;
        }

        readonly ITypeIdMapper _typeIdMapper;

        public IEventTypeToIdMapper IdMapper { get; private set; }

        readonly ISqlConnection _connectionManager;

        SqlConnection OpenConnection()
        {
            if (Transaction.Current == null)
            {
                this.Log().Warn($@"No ambient transaction. This is dangerous:
AT: 

{Environment.StackTrace}");
            }
            return _connectionManager.OpenConnection();
        }

        public void SetupSchemaIfDatabaseUnInitialized() => TransactionScopeCe.SuppressAmbientAndExecuteInNewTransaction(() =>
        {
            if(!_verifiedConnectionString)
            {
                using(var connection = OpenConnection())
                {
                    IdMapper = new SqlServerEventStoreEventTypeToIdMapper(_connectionManager, _typeIdMapper);

                    if(!_eventTable.Exists(connection))
                    {
                        _eventTypeTable.Create(connection);
                        _eventTable.Create(connection);
                    }

                    _verifiedConnectionString = true;
                }
            }
        });
    }
}
