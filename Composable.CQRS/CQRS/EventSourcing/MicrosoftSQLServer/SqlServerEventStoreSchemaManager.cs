using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using Composable.CQRS.CQRS.EventSourcing.Refactoring.Naming;
using Composable.CQRS.Logging.Log4Net;

namespace Composable.CQRS.CQRS.EventSourcing.MicrosoftSQLServer
{
    class SqlServerEventStoreSchemaManager
    {
        static readonly HashSet<string> VerifiedConnectionStrings = new HashSet<string>();
        static readonly Dictionary<string, IEventTypeToIdMapper> ConnectionIdMapper = new Dictionary<string, IEventTypeToIdMapper>();
        static readonly EventTableSchemaManager EventTable = new EventTableSchemaManager();
        static readonly EventTypeTableSchemaManager EventTypeTable = new EventTypeTableSchemaManager();
        static readonly LegacyEventTableSchemaManager LegacyEventTable = new LegacyEventTableSchemaManager();

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
            lock(VerifiedConnectionStrings)
            {
                if(VerifiedConnectionStrings.Contains(ConnectionString))
                {
                    IdMapper = ConnectionIdMapper[ConnectionString];
                    return;
                }

                using(var transaction = new TransactionScope())
                {
                    using(var connection = OpenConnection())
                    {
                        LegacyEventTable.LogAndThrowIfUsingLegacySchema(connection);
                        var usingLegacySchema = LegacyEventTable.IsUsingLegacySchema(connection);

                        IdMapper = new SqlServerEventStoreEventTypeToIdMapper(ConnectionString, _nameMapper);

                        ConnectionIdMapper[ConnectionString] = IdMapper;

                        if(!usingLegacySchema && !EventTable.Exists(connection))
                        {
                            EventTypeTable.Create(connection);
                            EventTable.Create(connection);
                        }

                        VerifiedConnectionStrings.Add(ConnectionString);
                    }
                    transaction.Complete();
                }
            }
        }

        //todo:remove the need for this by not using statics
        public static void ClearAllCache()
        {
            lock (VerifiedConnectionStrings)
            {
                foreach (var connectionString in VerifiedConnectionStrings.ToList())
                {
                    VerifiedConnectionStrings.Remove(connectionString);
                    ConnectionIdMapper.Remove(connectionString);
                }
            }
        }
    }
}
