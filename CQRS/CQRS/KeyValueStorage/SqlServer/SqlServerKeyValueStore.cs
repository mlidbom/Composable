#region usings

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using Composable.DDD;
using Composable.NewtonSoft;
using Composable.System;
using Newtonsoft.Json;

#endregion

namespace Composable.KeyValueStorage.SqlServer
{
    public class SqlServerKeyValueStore : IKeyValueStore
    {
        private readonly string _connectionString;
        private readonly SqlServerKeyValueStoreConfig _config;

        public SqlServerKeyValueStore(string connectionString, SqlServerKeyValueStoreConfig config = null)
        {
            if(config == null)
            {
                config = SqlServerKeyValueStoreConfig.Default;
            }
            _connectionString = connectionString;
            _config = config;
        }

        public IKeyValueStoreSession OpenSession()
        {
            return new SessionDisposeWrapper(new SqlServerKeyValueSession(this, _config));
        }

        private class SqlServerKeyValueSession : IKeyValueStoreSession, IEnlistmentNotification
        {
            private readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
                                                                       {
                                                                           
                                                                           TypeNameHandling = TypeNameHandling.Auto,
                                                                           ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                                                                           ContractResolver = new IncludeMembersWithPrivateSettersResolver()
                                                                       };

            private readonly SqlServerKeyValueStore _store;
            private readonly SqlServerKeyValueStoreConfig _config;
            private readonly Dictionary<Guid, object> _idMap = new Dictionary<Guid, object>();
            private readonly HashSet<Guid> _persistentValues = new HashSet<Guid>();
            private readonly SqlConnection _connection;            
            private bool _enlisted;
            private const int UniqueConstraintViolationErrorNumber = 2627;
            private int SqlBatchSize = 10;


            private static readonly HashSet<String> VerifiedTables = new HashSet<String>();
            private bool TableVerifiedToExist
            {
                get
                {
                    return VerifiedTables.Contains(_store._connectionString);
                }

                set
                {
                    if(value)
                    {
                        if(!TableVerifiedToExist)
                        {
                            VerifiedTables.Add(_store._connectionString);
                        }
                    }else if (TableVerifiedToExist)
                    {
                            VerifiedTables.Remove(_store._connectionString);
                    }               
                }
            }

            private static int instances;
            public SqlServerKeyValueSession(SqlServerKeyValueStore store, SqlServerKeyValueStoreConfig config)
            {
                //Console.WriteLine("{0}: {1}", GetType().Name, ++instances);
                _store = store;
                _config = config;
                _connection = new SqlConnection(store._connectionString);
                _connection.Open();
                EnsureTableExists();

                if(!config.Batching)
                {
                    SqlBatchSize = 1;
                }
            }

            private void EnsureTableExists()
            {
                if(!TableVerifiedToExist)
                {
                    using (var checkForTableCommand = _connection.CreateCommand())
                    {
                        checkForTableCommand.CommandText = "select count(*) from sys.tables where name = 'Store'";
                        var exists = (int)checkForTableCommand.ExecuteScalar();
                        if(exists == 0)
                        {
                            using (var createTableCommand = _connection.CreateCommand())
                            {

                                createTableCommand.CommandText =
                                    @"
CREATE TABLE [dbo].[Store](
	[Id] [uniqueidentifier] NOT NULL,
    [ValueType] [varchar](500) NOT NULL,
	[Value] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_Store] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
";
                                createTableCommand.ExecuteNonQuery();
                            }
                        }
                        TableVerifiedToExist = true;
                    }
                }
            }

            public bool TryGet<TValue>(Guid key, out TValue value)
            {
                value = default(TValue);
                EnlistInAmbientTransaction();

                object found;
                if(_idMap.TryGetValue(key, out found))
                {
                    value = (TValue)found;
                    return true;
                }

                using(var loadCommand = _connection.CreateCommand())
                {
                    loadCommand.CommandText = "SELECT Value, ValueType FROM Store WHERE Id=@Id";
                    loadCommand.Parameters.Add(new SqlParameter("Id", key));
                    using (var reader = loadCommand.ExecuteReader())
                    {
                        if(!reader.Read())
                        {
                            return false;
                        }
                        found = JsonConvert.DeserializeObject(reader.GetString(0), typeof(TValue), JsonSettings);
                    }
                }
                _persistentValues.Add(key);
                _idMap.Add(key, found);
                value = (TValue)found;
                return true;
            }

            public TValue Get<TValue>(Guid key)
            {
                TValue value;
                if (TryGet(key, out value))
                {
                    return value;
                }

                throw new NoSuchKeyException(key, typeof(TValue));
            }

            public IEnumerable<T> GetAll<T>()
            {
                EnlistInAmbientTransaction();

                using (var loadCommand = _connection.CreateCommand())
                {
                    loadCommand.CommandText = "SELECT Value, ValueType FROM Store WHERE ValueType=@ValueType";
                    loadCommand.Parameters.Add(new SqlParameter("ValueType", typeof(T).FullName));
                    using (var reader = loadCommand.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            yield return (T)JsonConvert.DeserializeObject((String)reader.GetString(0), typeof(T), JsonSettings);
                        }
                    }
                }
            }

            public void Save<TValue>(Guid key, TValue value)
            {
                EnlistInAmbientTransaction();

                object existing;
                if(_idMap.TryGetValue(key, out existing))
                {
                    throw new AttemptToSaveAlreadyPersistedValueException(key, value);
                }
                _idMap.Add(key, value);
            }

            public void Save<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
            {
                Save(entity.Id, entity);
            }

            public void Delete<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
            {
                EnlistInAmbientTransaction();
                _idMap.Remove(entity.Id);

                using (var command = _connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;                
                    command.CommandText += "DELETE Store WHERE Id = @Id";
                    command.Parameters.Add(new SqlParameter("Id", entity.Id));
                    var rowsAffected = command.ExecuteNonQuery();
                    if(rowsAffected == 0)
                    {
                        throw new NoSuchKeyException(entity.Id, entity.GetType());
                    }if(rowsAffected > 1)
                    {
                        throw new TooManyItemsDeletedException();
                    }
                }

            }

            public void SaveChanges()
            {
                Log("saving changes in: {0}", Me);
                InsertValues(_idMap.Where(entry => !_persistentValues.Contains(entry.Key)));
                UpdateValues(_idMap.Where(entry => _persistentValues.Contains(entry.Key)));
            }



            private readonly HashSet<Transaction> enlistedIn = new HashSet<Transaction>();
            private readonly Guid Me = Guid.NewGuid();
            private void EnlistInAmbientTransaction()
            {
                if (Transaction.Current != null && !enlistedIn.Contains(Transaction.Current))
                {
                    Transaction.Current.EnlistVolatile(this, EnlistmentOptions.EnlistDuringPrepareRequired);
                    enlistedIn.Add(Transaction.Current);
                    _connection.EnlistTransaction(Transaction.Current);
                    Log("enlisting in local: {0} in {1}", Me, Transaction.Current.TransactionInformation.LocalIdentifier);
                    Log("enlisting in distributed: {0} in {1}", Me, Transaction.Current.TransactionInformation.DistributedIdentifier);
                    Log("enlistments: {0} for {1}", enlistedIn.Count, Me);                    
                }
            }

            private void UpdateValues(IEnumerable<KeyValuePair<Guid, object>> values)
            {
                var handled = 0;
                var eventCount = values.Count();
                while(handled < eventCount)
                {
                    using(var command = _connection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;
                        for(var handledInBatch = 0; handledInBatch < SqlBatchSize && handled < eventCount; handledInBatch++, handled++)
                        {
                            var entry = values.ElementAt(handledInBatch);

                            command.CommandText += "UPDATE Store SET Value = @Value{0} WHERE Id = @Id{0}\n"
                                .FormatWith(handledInBatch);

                            command.Parameters.Add(new SqlParameter("Id" + handledInBatch, entry.Key));
                            command.Parameters.Add(new SqlParameter("Value" + handledInBatch,
                                                                    JsonConvert.SerializeObject(entry.Value, _config.JSonFormatting, JsonSettings)));
                        }
                        command.ExecuteNonQuery();
                    }
                }
            }

            private void InsertValues(IEnumerable<KeyValuePair<Guid, object>> values)
            {
                var handled = 0;
                var eventCount = values.Count();
                while(handled < eventCount)
                {
                    using(var command = _connection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;
                        KeyValuePair<Guid, object> entry = new KeyValuePair<Guid, object>();
                        for(var handledInBatch = 0; handledInBatch < SqlBatchSize && handled < eventCount; handledInBatch++, handled++)
                        {
                            entry = values.ElementAt(handledInBatch);

                            command.CommandText += "INSERT Store(Id, ValueType, Value) VALUES(@Id{0}, @ValueType{0}, @Value{0})"
                                .FormatWith(handledInBatch);

                            command.Parameters.Add(new SqlParameter("Id" + handledInBatch, entry.Key));
                            command.Parameters.Add(new SqlParameter("ValueType" + handledInBatch, entry.Value.GetType().FullName));
                            command.Parameters.Add(new SqlParameter("Value" + handledInBatch,
                                                                    JsonConvert.SerializeObject(entry.Value, _config.JSonFormatting, JsonSettings)));
                        }
                        try
                        {
                            command.ExecuteNonQuery();
                        }
                        catch(SqlException e)
                        {
                            if(e.Number == UniqueConstraintViolationErrorNumber)
                            {
                                if (SqlBatchSize == 1)
                                {
                                    throw new AttemptToSaveAlreadyPersistedValueException(entry.Key, entry.Value);
                                }
                                
                                throw new AttemptToSaveAlreadyPersistedValueException(Guid.Empty, "Batched insert cannot extract value try with SqlServerKeyValueStoreConfig.NoBatching...");
                            }
                            throw;
                        }
                    }
                }
            }

            private bool _disposed;
            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    Log("disposing {0}", Me);
                    //Console.WriteLine("{0}: {1}", GetType().Name, --instances);
                    _connection.Dispose();
                    _idMap.Clear();
                }
            }

            void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
            {
                try
                {
                    Log("prepare called on {0} with {1} changes from transaction", Me, _idMap.Count);
                    
                    SaveChanges();

                    preparingEnlistment.Prepared();
                    Log("prepare completed on {0}", Me);
                }
                catch (Exception e)
                {
                    Log("prepare failed on {0}", e);
                    preparingEnlistment.ForceRollback(e);
                }
            }

            void IEnlistmentNotification.Commit(Enlistment enlistment)
            {
                try
                {
                    Log("commit called on {0}", Me);
                    _enlisted = false;
                    enlistment.Done();
                    HandleScheduledDispose();
                }
                catch (Exception e)
                {
                    Log("commit failed on {0}", e);
                }
            }

            void IEnlistmentNotification.Rollback(Enlistment enlistment)
            {
                try
                {
                    Log("rollback called on {0}", Me);
                    _enlisted = false;
                    HandleScheduledDispose();
                }
                catch (Exception e)
                {
                    Log("rollback failed on {0}", e);
                }
            }

            public void InDoubt(Enlistment enlistment)
            {
                try
                {
                    Log("indoubt called on {0}", Me);
                    _enlisted = false;
                    enlistment.Done();
                    HandleScheduledDispose();
                }
                catch(Exception e)
                {
                    Log("Indoubt failed on {0}", e);
                }
            }


            private void HandleScheduledDispose()
            {
                if(_scheduledForDisposeAfterTransactionDone)
                {
                    _scheduledForDisposeAfterTransactionDone = false;
                    Dispose();
                }
            }

            private bool _scheduledForDisposeAfterTransactionDone;

            public void DisposeIfNotEnlisted()
            {
                Log("attempting dispose for {0}", Me);
                if(enlistedIn.Any())
                {
                    Log("scheduling dispose for {0}", Me);
                    _scheduledForDisposeAfterTransactionDone = true;
                }
                else
                {                    
                    Dispose();
                }
            }

            public void PurgeDB()
            {
                using (var dropCommand = _connection.CreateCommand())
                {
                    dropCommand.CommandText =
                        @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Store]') AND type in (N'U'))
DROP TABLE [dbo].[Store]";

                    dropCommand.ExecuteNonQuery();
                    TableVerifiedToExist = false;
                }
            }

            private void Log(string message, params object[] @params)
            {
                //Console.WriteLine("{0} : ".FormatWith(GetType().Name) + " " + message, @params);
            }

        }

        private class SessionDisposeWrapper : KeyValueStoreSessionProxy
        {
            private readonly SqlServerKeyValueSession _session;
            public SessionDisposeWrapper(SqlServerKeyValueSession session) : base(session)
            {
                _session = session;
            }

            public override void Dispose()
            {                
                _session.DisposeIfNotEnlisted();
            }
        }

        public static void ResetDB(string connectionString)
        {
            var me = new SqlServerKeyValueStore(connectionString);
            using (var session = new SqlServerKeyValueSession(me, SqlServerKeyValueStoreConfig.Default))
            {
                session.PurgeDB();
            }
        }
    }

    public class SqlServerKeyValueStoreConfig
    {
        public static readonly SqlServerKeyValueStoreConfig Default = new SqlServerKeyValueStoreConfig
                                                                          {};

        public bool Batching = true;
        public Formatting JSonFormatting = Formatting.None;
    }
}