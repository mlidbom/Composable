using System;
using System.Data.SqlClient;
using System.Transactions;
using Composable.Logging.Log4Net;
using Composable.Persistence.EventStore.Refactoring.Naming;
using Composable.System.Data.SqlClient;

namespace Composable.Persistence.EventStore.MicrosoftSQLServer
{
    class SqlServerEventStoreSchemaManager : IEventStoreSchemaManager
    {
        bool _verifiedConnectionString;
        readonly EventTableSchemaManager _eventTable = new EventTableSchemaManager();
        readonly EventTypeTableSchemaManager _eventTypeTable = new EventTypeTableSchemaManager();
        public SqlServerEventStoreSchemaManager(ISqlConnectionProvider connectionString, IEventNameMapper nameMapper)
        {
            _connectionManager = connectionString;
            _nameMapper = nameMapper;
        }

        readonly IEventNameMapper _nameMapper;

        public IEventTypeToIdMapper IdMapper { get; private set; }

        readonly ISqlConnectionProvider _connectionManager;

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

        public void SetupSchemaIfDatabaseUnInitialized()
        {
            if(!_verifiedConnectionString)
            {
                using(var transaction = new TransactionScope())
                {
                    using(var connection = OpenConnection())
                    {
                        IdMapper = new SqlServerEventStoreEventTypeToIdMapper(_connectionManager, _nameMapper);

                        if(!_eventTable.Exists(connection))
                        {
                            _eventTypeTable.Create(connection);
                            _eventTable.Create(connection);
                        }

                        _verifiedConnectionString = true;
                    }
                    transaction.Complete();
                }
            }
        }
    }
}
