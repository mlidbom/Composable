using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Transactions;
using Composable.CQRS.EventSourcing.EventRefactoring;
using Composable.Logging.Log4Net;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    [Obsolete("Search and replace: 'using Composable.CQRS.EventSourcing.SQLServer;' with 'using Composable.CQRS.EventSourcing.MicrosoftSQLServer;' this type is only still around for binary compatibility.", error: true)]
    internal class SqlServerEventStoreSchemaManager
    {
        private static readonly HashSet<string> VerifiedConnectionStrings = new HashSet<string>();
        private static readonly Dictionary<string, IEventTypeToIdMapper> ConnectionIdMapper = new Dictionary<string, IEventTypeToIdMapper>();
        private static readonly EventTableSchemaManager EventTable = new EventTableSchemaManager();
        private static readonly EventTypeTableSchemaManager EventTypeTable = new EventTypeTableSchemaManager();
        private static readonly LegacyEventTableSchemaManager LegacyEventTable = new LegacyEventTableSchemaManager();

        public SqlServerEventStoreSchemaManager(string connectionString, IEventNameMapper nameMapper)
        {
            ConnectionString = connectionString;
            _nameMapper = nameMapper;
        }

        private readonly IEventNameMapper _nameMapper;

        public string EventTableName => UsingLegacySchema
                                            ? LegacyEventTable.Name
                                            : EventTable.Name;

        private bool UsingLegacySchema => IdMapper is LegacySchemaSqlServerEventStoreEventTypeToIdMapper;

        public IEventTypeToIdMapper IdMapper { get; private set; }

        private string ConnectionString { get; }
        public string InsertColumnsAddendum => UsingLegacySchema ? "" : $", {SQLServer.EventTable.Columns.SqlTimeStamp}";
        public string InsertValuesAddendum => UsingLegacySchema ? "" : ", GETDATE()";

        public string InsertionOrderColumn => UsingLegacySchema
                                                  ? SQLServer.LegacyEventTable.Columns.SqlTimeStamp
                                                  : SQLServer.EventTable.Columns.InsertionOrder;

        private SqlConnection OpenConnection()
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
            lock(VerifiedConnectionStrings)
            {
                if(VerifiedConnectionStrings.Contains(ConnectionString))
                {
                    IdMapper = ConnectionIdMapper[ConnectionString];
                    return;
                }

                using(var connection = OpenConnection())
                {
                    LegacyEventTable.LogWarningIfUsingLegacySqlSchema(connection);
                    var usingLegacySchema = LegacyEventTable.IsUsingLegacySchema(connection);

                    IdMapper = usingLegacySchema
                                   ? (IEventTypeToIdMapper)new LegacySchemaSqlServerEventStoreEventTypeToIdMapper(_nameMapper)
                                   : (IEventTypeToIdMapper)new SqlServerEventStoreEventTypeToIdMapper(ConnectionString, _nameMapper);

                    ConnectionIdMapper[ConnectionString] = IdMapper;

                    if(!usingLegacySchema && !EventTable.Exists(connection))
                    {
                        EventTypeTable.Create(connection);
                        EventTable.Create(connection);
                    }

                    VerifiedConnectionStrings.Add(ConnectionString);
                }
            }
        }

        public static void ResetDB(string connectionString)
        {
            new SqlServerEventStoreSchemaManager(connectionString, new DefaultEventNameMapper()).ResetDB();
        }

        public void ResetDB()
        {
            lock(VerifiedConnectionStrings)
            {
                using(var connection = OpenConnection())
                {
                    EventTable.DropIfExists(connection);
                    EventTypeTable.DropIfExists(connection);
                }
                VerifiedConnectionStrings.Remove(ConnectionString);
                SetupSchemaIfDatabaseUnInitialized();
            }
        }
    }
}
