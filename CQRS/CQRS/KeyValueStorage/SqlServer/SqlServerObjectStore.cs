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
    public class SqlServerObjectStore : IObjectStore
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SqlServerObjectStore));

        private readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
                                                                   {
                                                                           
                                                                       TypeNameHandling = TypeNameHandling.Auto,
                                                                       ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                                                                       ContractResolver = new IncludeMembersWithPrivateSettersResolver()
                                                                   };

        private readonly SqlServerKeyValueStore _store;
        private readonly SqlServerKeyValueStoreConfig _config;

        private readonly HashSet<Guid> _persistentValues = new HashSet<Guid>();
        private const int UniqueConstraintViolationErrorNumber = 2627;
        private readonly int SqlBatchSize = 10;

        public SqlServerObjectStore(SqlServerKeyValueStore store)
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


        public bool TryGet<TValue>(Guid key, out TValue value)
        {
            value = default(TValue);

            object found;
            using (var connection = OpenSession())
            {
                using(var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT Value, ValueType FROM Store WHERE Id=@Id AND ValueType=@ValueType";
                    command.Parameters.Add(new SqlParameter("Id", key));
                    command.Parameters.Add(new SqlParameter("ValueType", typeof(TValue).FullName));
                    using(var reader = command.ExecuteReader())
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
            value = (TValue)found;
            return true;
        }

        public void Add<T>(Guid id, T value)
        {
            using (var connection = OpenSession())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;

                    command.CommandText += "INSERT Store(Id, ValueType, Value) VALUES(@Id, @ValueType, @Value)";

                    command.Parameters.Add(new SqlParameter("Id", id));
                    command.Parameters.Add(new SqlParameter("ValueType", value.GetType().FullName));
                    command.Parameters.Add(new SqlParameter("Value",
                                                            JsonConvert.SerializeObject(value, _config.JSonFormatting, JsonSettings)));
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException e)
                    {
                        if (e.Number == UniqueConstraintViolationErrorNumber)
                        {
                            throw new AttemptToSaveAlreadyPersistedValueException(id, value);
                        }
                        throw;
                    }
                }
            }
        }

        public bool Remove<T>(Guid id)
        {
            using (var _connection = OpenSession())
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText += "DELETE Store WHERE Id = @Id AND ValueType = @ValueType";
                    command.Parameters.Add(new SqlParameter("Id", id));
                    command.Parameters.Add(new SqlParameter("ValueType", typeof (T).FullName));
                    var rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 1)
                    {
                        throw new TooManyItemsDeletedException();
                    }
                    return rowsAffected > 0;
                }
            }
        }

        public void Update(IEnumerable<KeyValuePair<Guid, object>> values)
        {
            using (var _connection = OpenSession())
            {
                var handled = 0;
                var eventCount = values.Count();
                while (handled < eventCount)
                {
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;
                        for (var handledInBatch = 0; handledInBatch < SqlBatchSize && handled < eventCount; handledInBatch++, handled++)
                        {
                            var entry = values.ElementAt(handledInBatch);

                            command.CommandText += "UPDATE Store SET Value = @Value{0} WHERE Id = @Id{0} AND ValueType=@ValueType{0}\n"
                                .FormatWith(handledInBatch);

                            command.Parameters.Add(new SqlParameter("Id" + handledInBatch, entry.Key));
                            command.Parameters.Add(new SqlParameter("ValueType" + handledInBatch, entry.Value.GetType().FullName));
                            command.Parameters.Add(new SqlParameter("Value" + handledInBatch,
                                                                    JsonConvert.SerializeObject(entry.Value, _config.JSonFormatting, JsonSettings)));
                        }
                        command.ExecuteNonQuery();
                    }
                }
            }
        }


        IEnumerable<KeyValuePair<Guid, T>> IObjectStore.GetAll<T>()
        {
            using (var _connection = OpenSession())
            {
                using(var loadCommand = _connection.CreateCommand())
                {
                    loadCommand.CommandText = "SELECT Id, Value, ValueType FROM Store WHERE ValueType=@ValueType";
                    loadCommand.Parameters.Add(new SqlParameter("ValueType", typeof(T).FullName));
                    using(var reader = loadCommand.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            yield return
                                new KeyValuePair<Guid, T>(reader.GetGuid(0),
                                (T)JsonConvert.DeserializeObject(reader.GetString(1), typeof(T), JsonSettings));
                        }
                    }
                }
            }
        }

        private readonly Guid Me = Guid.NewGuid();

        private bool _disposed;
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Log.DebugFormat("disposing {0}", Me);
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
                if (!TableVerifiedToExist)
                {
                    using (var checkForTableCommand = _connection.CreateCommand())
                    {
                        checkForTableCommand.CommandText = "select count(*) from sys.tables where name = 'Store'";
                        var exists = (int)checkForTableCommand.ExecuteScalar();
                        if (exists == 0)
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
	[ValueType], [Id] ASC
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

        private static readonly HashSet<String> VerifiedTables = new HashSet<String>();
        private bool TableVerifiedToExist
        {
            get
            {
                return VerifiedTables.Contains(_store.ConnectionString);
            }

            set
            {
                if (value)
                {
                    if (!TableVerifiedToExist)
                    {
                        VerifiedTables.Add(_store.ConnectionString);
                    }
                }
                else if (TableVerifiedToExist)
                {
                    VerifiedTables.Remove(_store.ConnectionString);
                }
            }
        }

        public void PurgeDb()
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