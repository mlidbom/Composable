﻿using System;
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
        public SqlServerEventStoreSchemaManager(ISqlConnection connectionString, ITypeMapper typeMapper)
        {
            _connectionManager = connectionString;
            _typeMapper = typeMapper;
        }

        readonly ITypeMapper _typeMapper;

        public IEventTypeToIdMapper IdMapper { get; private set; }

        readonly ISqlConnection _connectionManager;

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
                using(var connection = OpenConnection())
                {
                    IdMapper = new SqlServerEventStoreEventTypeToIdMapper(_connectionManager, _typeMapper);

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
