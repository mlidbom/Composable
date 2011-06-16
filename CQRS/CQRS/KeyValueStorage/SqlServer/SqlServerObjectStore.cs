using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Composable.DDD;
using Composable.NewtonSoft;
using Composable.System;
using Composable.System.Linq;
using Newtonsoft.Json;
using log4net;
using Composable.System.Reflection;

namespace Composable.KeyValueStorage.SqlServer
{
    public class SqlServerObjectStore : IObjectStore
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SqlServerObjectStore));

        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
                                                                   {
                                                                           
                                                                       TypeNameHandling = TypeNameHandling.Auto,
                                                                       ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                                                                       ContractResolver = new IncludeMembersWithPrivateSettersResolver()
                                                                   };

        private readonly SqlServerDocumentDb _store;
        private readonly SqlServerDocumentDbConfig _config;

        private readonly HashSet<string> _persistentValues = new HashSet<string>();
        private const int UniqueConstraintViolationErrorNumber = 2627;
        private readonly int _sqlBatchSize = 10;

        private static readonly ISet<Type> KnownTypes = new HashSet<Type>();

        private static readonly object LockObject = new object();

        public SqlServerObjectStore(SqlServerDocumentDb store)
        {
            Log.Debug("Constructor called");
            _store = store;
            _config = _store.Config;

            EnsureInitialized();


            if (!_store.Config.Batching)
            {
                _sqlBatchSize = 1;
            }
        }


        public bool TryGet<TValue>(object key, out TValue value)
        {
            EnsureTypeRegistered(typeof(TValue));
            value = default(TValue);

            object found;
            using (var connection = OpenSession())
            {
                using(var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT Value, ValueType FROM Store WHERE Id=@Id AND ValueType ";
                    command.Parameters.Add(new SqlParameter("Id", key.ToString()));
                    
                    AddTypeCriteria(command, typeof(TValue));

                    using(var reader = command.ExecuteReader())
                    {
                        if(!reader.Read())
                        {
                            return false;
                        }
                        found = JsonConvert.DeserializeObject(reader.GetString(0), reader.GetString(1).AsType(), _jsonSettings);
                    }
                }
            }
            _persistentValues.Add(key.ToString());
            value = (TValue)found;
            return true;
        }

        public void Add<T>(object id, T value)
        {
            EnsureTypeRegistered(value.GetType());
            using (var connection = OpenSession())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;

                    command.CommandText += "INSERT Store(Id, ValueType, Value) VALUES(@Id, @ValueType, @Value)";

                    command.Parameters.Add(new SqlParameter("Id", id.ToString()));
                    command.Parameters.Add(new SqlParameter("ValueType", value.GetType().FullName));

                    command.Parameters.Add(new SqlParameter("Value",
                                                            JsonConvert.SerializeObject(value, _config.JSonFormatting, _jsonSettings)));
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

        public bool Remove<T>(object id)
        {
            using (var connection = OpenSession())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText += "DELETE Store WHERE Id = @Id AND ValueType ";
                    command.Parameters.Add(new SqlParameter("Id", id.ToString()));
                    
                    AddTypeCriteria(command, typeof(T));
                    
                    var rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 1)
                    {
                        throw new TooManyItemsDeletedException();
                    }
                    return rowsAffected > 0;
                }
            }
        }

        public void Update(IEnumerable<KeyValuePair<object, object>> values)
        {
            values = values.ToList();
            using (var connection = OpenSession())
            {
                var handled = 0;
                var eventCount = values.Count();
                while (handled < eventCount)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;
                        for (var handledInBatch = 0; handledInBatch < _sqlBatchSize && handled < eventCount; handledInBatch++, handled++)
                        {
                            var entry = values.ElementAt(handledInBatch);

                            command.CommandText += "UPDATE Store SET Value = @Value{0} WHERE Id = @Id{0} AND ValueType \n"
                                .FormatWith(handledInBatch);
                            command.Parameters.Add(new SqlParameter("Id" + handledInBatch, entry.Key.ToString()));
                            
                            AddTypeCriteria(command, entry.Value.GetType());

                            command.Parameters.Add(new SqlParameter("Value" + handledInBatch,
                                                                    JsonConvert.SerializeObject(entry.Value, _config.JSonFormatting, _jsonSettings)));
                        }
                        command.ExecuteNonQuery();
                    }
                }
            }
        }


        IEnumerable<KeyValuePair<Guid, T>> IObjectStore.GetAll<T>()
        {
            using (var connection = OpenSession())
            {
                using(var loadCommand = connection.CreateCommand())
                {
                    loadCommand.CommandText = "SELECT Id, Value, ValueType FROM Store WHERE ValueType ";
                    
                    AddTypeCriteria(loadCommand, typeof(T));

                    using(var reader = loadCommand.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            yield return
                                new KeyValuePair<Guid, T>(Guid.Parse(reader.GetString(0)),
                                (T)JsonConvert.DeserializeObject(reader.GetString(1), reader.GetString(2).AsType(), _jsonSettings));
                        }
                    }
                }
            }
        }

        private readonly Guid _me = Guid.NewGuid();

        private bool _disposed;
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Log.DebugFormat("disposing {0}", _me);
            }
        }

        private SqlConnection OpenSession()
        {
            var connection = new SqlConnection(_store.ConnectionString);
            connection.Open();
            return connection;
        }

        private void EnsureTypeRegistered(Type type)
        {
            lock (LockObject)
            {
                if (!KnownTypes.Contains(type))
                {
                    KnownTypes.Add(type);
                }
            }
        }

        private void AddTypeCriteria(SqlCommand command, Type type)
        {
            lock (LockObject)
            {
                var acceptableTypeNames = KnownTypes.Where(type.IsAssignableFrom).Select(t => t.FullName).ToArray();
                if (acceptableTypeNames.None())
                {
                    throw new Exception("FUBAR");
                }

                command.CommandText += " IN( '" + acceptableTypeNames.Join("','") + "')";
            }
        }

        private void EnsureInitialized()
        {
            lock (LockObject)
            {
                using (var connection = OpenSession())
                {
                    if (!TableVerifiedToExist)
                    {
                        using (var checkForTableCommand = connection.CreateCommand())
                        {
                            checkForTableCommand.CommandText = "select count(*) from sys.tables where name = 'Store'";
                            var exists = (int) checkForTableCommand.ExecuteScalar();
                            if (exists == 0)
                            {
                                using (var createTableCommand = connection.CreateCommand())
                                {

                                    createTableCommand.CommandText =
                                        @"
CREATE TABLE [dbo].[Store](
	[Id] [nvarchar](500) NOT NULL,
    [ValueType] [varchar](500) NOT NULL,
	[Value] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_Store] PRIMARY KEY CLUSTERED 
(
	[Id], [ValueType] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

CREATE NONCLUSTERED INDEX [IX_ValueType] ON [dbo].[Store] 
(
	[ValueType] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]

";
                                    createTableCommand.ExecuteNonQuery();
                                }
                            }
                            TableVerifiedToExist = true;
                        }

                        using (var findTypesCommand = connection.CreateCommand())
                        {
                            findTypesCommand.CommandText = "SELECT DISTINCT ValueType FROM Store";
                            using (var reader = findTypesCommand.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    KnownTypes.Add(reader.GetString(0).AsType());
                                }
                            }
                        }
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
            lock (LockObject)
            {
                using (var connection = OpenSession())
                {
                    using (var dropCommand = connection.CreateCommand())
                    {
                        dropCommand.CommandText =
                            @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Store]') AND type in (N'U'))
DROP TABLE [dbo].[Store]";

                        dropCommand.ExecuteNonQuery();
                        TableVerifiedToExist = false;
                        EnsureInitialized();
                    }
                }
            }
        }
    }
}