using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Composable.DDD;
using Composable.NewtonSoft;
using Composable.System;
using Newtonsoft.Json;
using log4net;

namespace Composable.KeyValueStorage.SqlServer
{
    public class SqlServerKeyValueSession : IKeyValueStoreSession
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SqlServerKeyValueSession));

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
        private const int UniqueConstraintViolationErrorNumber = 2627;
        private readonly int SqlBatchSize = 10;

        public SqlServerKeyValueSession(SqlServerKeyValueStore store)
        {
            Log.Debug("Constructor called");
            _store = store;
            _config = _store.Config;

            EnsureTableExists();


            if (!_store.Config.Batching)
            {
                SqlBatchSize = 1;
            }
        }



        private static readonly HashSet<String> VerifiedTables = new HashSet<String>();
        private bool TableVerifiedToExist
        {
            get
            {
                return VerifiedTables.Contains(_store.ConnectionString);
            }

            set
            {
                if(value)
                {
                    if(!TableVerifiedToExist)
                    {
                        VerifiedTables.Add(_store.ConnectionString);
                    }
                }else if (TableVerifiedToExist)
                {
                    VerifiedTables.Remove(_store.ConnectionString);
                }               
            }
        }

        private SqlConnection OpenSession()
        {
            var connection = new SqlConnection(_store.ConnectionString);
            connection.Open();
            return connection;
        }

        private void EnsureTableExists()
        {
            using (var _connection = OpenSession())
            {
                if(!TableVerifiedToExist)
                {
                    using(var checkForTableCommand = _connection.CreateCommand())
                    {
                        checkForTableCommand.CommandText = "select count(*) from sys.tables where name = 'Store'";
                        var exists = (int)checkForTableCommand.ExecuteScalar();
                        if(exists == 0)
                        {
                            using(var createTableCommand = _connection.CreateCommand())
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
        }

        public bool TryGet<TValue>(Guid key, out TValue value)
        {
            value = default(TValue);

            object found;
            if(_idMap.TryGetValue(key, out found))
            {
                value = (TValue)found;
                return true;
            }
            using (var _connection = OpenSession())
            {
                using(var loadCommand = _connection.CreateCommand())
                {
                    loadCommand.CommandText = "SELECT Value, ValueType FROM Store WHERE Id=@Id";
                    loadCommand.Parameters.Add(new SqlParameter("Id", key));
                    using(var reader = loadCommand.ExecuteReader())
                    {
                        if(!reader.Read())
                        {
                            return false;
                        }
                        found = JsonConvert.DeserializeObject(reader.GetString(0), typeof(TValue), JsonSettings);
                    }
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
            using (var _connection = OpenSession())
            {
                using(var loadCommand = _connection.CreateCommand())
                {
                    loadCommand.CommandText = "SELECT Value, ValueType FROM Store WHERE ValueType=@ValueType";
                    loadCommand.Parameters.Add(new SqlParameter("ValueType", typeof(T).FullName));
                    using(var reader = loadCommand.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            yield return (T)JsonConvert.DeserializeObject(reader.GetString(0), typeof(T), JsonSettings);
                        }
                    }
                }
            }
        }

        public void Save<TValue>(Guid id, TValue value)
        {
            object existing;
            if(_idMap.TryGetValue(id, out existing))
            {
                throw new AttemptToSaveAlreadyPersistedValueException(id, value);
            }
            _idMap.Add(id, value);
        }

        public void Save<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            Save(entity.Id, entity);
        }

        public void Delete<TEntity>(Guid id)
        {
            throw new NotImplementedException();
        }

        public void Delete<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            _idMap.Remove(entity.Id);
            using (var _connection = OpenSession())
            {
                using(var command = _connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText += "DELETE Store WHERE Id = @Id";
                    command.Parameters.Add(new SqlParameter("Id", entity.Id));
                    var rowsAffected = command.ExecuteNonQuery();
                    if(rowsAffected == 0)
                    {
                        throw new NoSuchKeyException(entity.Id, entity.GetType());
                    }
                    if(rowsAffected > 1)
                    {
                        throw new TooManyItemsDeletedException();
                    }
                }
            }

        }

        public void SaveChanges()
        {
            Log.DebugFormat("saving changes in: {0}", Me);
            InsertValues(_idMap.Where(entry => !_persistentValues.Contains(entry.Key)));
            UpdateValues(_idMap.Where(entry => _persistentValues.Contains(entry.Key)));
        }



        private readonly Guid Me = Guid.NewGuid();


        private void UpdateValues(IEnumerable<KeyValuePair<Guid, object>> values)
        {
            using (var _connection = OpenSession())
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
        }

        private void InsertValues(IEnumerable<KeyValuePair<Guid, object>> values)
        {
            using (var _connection = OpenSession())
            {
                var handled = 0;
                var eventCount = values.Count();
                while(handled < eventCount)
                {
                    using(var command = _connection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;
                        var entry = new KeyValuePair<Guid, object>();
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
                                if(SqlBatchSize == 1)
                                {
                                    throw new AttemptToSaveAlreadyPersistedValueException(entry.Key, entry.Value);
                                }

                                throw new AttemptToSaveAlreadyPersistedValueException(Guid.Empty,
                                                                                      "Batched insert cannot extract value try with SqlServerKeyValueStoreConfig.NoBatching...");
                            }
                            throw;
                        }
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
                Log.DebugFormat("disposing {0}", Me);
                //Console.WriteLine("{0}: {1}", GetType().Name, --instances);
                _idMap.Clear();
            }
        }            

        public void PurgeDB()
        {
            using (var _connection = OpenSession())
            {
                using(var dropCommand = _connection.CreateCommand())
                {
                    dropCommand.CommandText =
                        @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Store]') AND type in (N'U'))
DROP TABLE [dbo].[Store]";

                    dropCommand.ExecuteNonQuery();
                    TableVerifiedToExist = false;
                }
            }
        }
    }
}