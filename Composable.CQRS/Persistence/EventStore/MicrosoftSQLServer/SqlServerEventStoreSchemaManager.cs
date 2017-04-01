using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Transactions;
using Composable.Logging.Log4Net;
using Composable.Persistence.EventStore.Refactoring.Naming;

namespace Composable.Persistence.EventStore.MicrosoftSQLServer
{
    class SqlServerEventStoreSchemaManager : IEventStoreSchemaManager
    {
        readonly HashSet<string> _verifiedConnectionStrings = new HashSet<string>();
        readonly Dictionary<string, IEventTypeToIdMapper> _connectionIdMapper = new Dictionary<string, IEventTypeToIdMapper>();
        readonly EventTableSchemaManager _eventTable = new EventTableSchemaManager();
        readonly EventTypeTableSchemaManager _eventTypeTable = new EventTypeTableSchemaManager();
        public SqlServerEventStoreSchemaManager(string connectionString, IEventNameMapper nameMapper)
        {
            ConnectionString = connectionString;
            _nameMapper = nameMapper;
        }

        readonly IEventNameMapper _nameMapper;


        public IEventTypeToIdMapper IdMapper { get; private set; }

        string ConnectionString { get; }

        SqlConnection OpenConnection()
        {
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            if(Transaction.Current == null)
            {
                this.Log().Warn($@"No ambient transaction. This is dangerous:
AT: 

{Environment.StackTrace}");
            }
            return connection;
        }

        public void SetupSchemaIfDatabaseUnInitialized()
        {
            lock(_verifiedConnectionStrings)
            {
                if(_verifiedConnectionStrings.Contains(ConnectionString))
                {
                    IdMapper = _connectionIdMapper[ConnectionString];
                    return;
                }

                using(var transaction = new TransactionScope())
                {
                    using(var connection = OpenConnection())
                    {
                        IdMapper = new SqlServerEventStoreEventTypeToIdMapper(ConnectionString, _nameMapper);

                        _connectionIdMapper[ConnectionString] = IdMapper;

                        if(!_eventTable.Exists(connection))
                        {
                            _eventTypeTable.Create(connection);
                            _eventTable.Create(connection);
                        }

                        _verifiedConnectionStrings.Add(ConnectionString);
                    }
                    transaction.Complete();
                }
            }
        }
    }
}
