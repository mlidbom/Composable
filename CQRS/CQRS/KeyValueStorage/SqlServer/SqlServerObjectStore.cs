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
using Composable.System.Collections.Collections;

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

        private readonly Dictionary<Type, Dictionary<string, string>> _persistentValues = new Dictionary<Type, Dictionary<string,string>>();
        private const int UniqueConstraintViolationErrorNumber = 2627;

        private static readonly ISet<Type> KnownTypes = new HashSet<Type>();

        private static readonly object LockObject = new object();

        public SqlServerObjectStore(SqlServerDocumentDb store)
        {
            _store = store;
            _config = _store.Config;

            EnsureInitialized(_store.ConnectionString);
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
                    string lockHint = DocumentDbSession.UseUpdateLock ? "With(XLOCK)" : "";
                    command.CommandText = "SELECT Value, ValueType FROM Store {0} WHERE Id=@Id AND ValueType ".FormatWith(lockHint);
                    command.Parameters.Add(new SqlParameter("Id", key.ToString()));
                    
                    AddTypeCriteria(command, typeof(TValue));

                    using(var reader = command.ExecuteReader())
                    {
                        if(!reader.Read())
                        {
                            return false;
                        }
                        var stringValue = reader.GetString(0);
                        found = JsonConvert.DeserializeObject(stringValue, reader.GetString(1).AsType(), _jsonSettings);
                        _persistentValues.GetOrAddDefault(found.GetType())[key.ToString()] = stringValue;
                    }
                }
            }
            
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

                    var stringValue = JsonConvert.SerializeObject(value, _config.JSonFormatting, _jsonSettings);
                    command.Parameters.Add(new SqlParameter("Value", stringValue));

                    _persistentValues.GetOrAddDefault(value.GetType())[id.ToString()] = stringValue;
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

        public void Update(IEnumerable<KeyValuePair<string, object>> values)
        {
            values = values.ToList();
            using (var connection = OpenSession())
            {
                foreach (var entry in values)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;
                        var stringValue = JsonConvert.SerializeObject(entry.Value, _config.JSonFormatting, _jsonSettings);

                        string oldValue;
                        var needsUpdate = !_persistentValues.GetOrAddDefault(entry.Value.GetType()).TryGetValue(entry.Key, out oldValue) || stringValue != oldValue;
                        if (needsUpdate)
                        {
                            _persistentValues.GetOrAddDefault(entry.Value.GetType())[entry.Key] = stringValue;
                            command.CommandText += "UPDATE Store SET Value = @Value WHERE Id = @Id AND ValueType \n";
                            command.Parameters.Add(new SqlParameter("Id", entry.Key));

                            AddTypeCriteria(command, entry.Value.GetType());

                            command.Parameters.Add(new SqlParameter("Value", stringValue));
                        }
                        if (!command.CommandText.IsNullOrWhiteSpace())
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
        }


        IEnumerable<KeyValuePair<Guid, T>> IObjectStore.GetAll<T>()
        {
            if(KnownTypes.None( t => typeof(T).IsAssignableFrom(t)))
            {
                yield break;    
            }

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

        private bool _disposed;
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        private SqlConnection OpenSession()
        {
            return OpenSession(_store.ConnectionString);
        }

        private static SqlConnection OpenSession(string connectionString)
        {
            var connection = new SqlConnection(connectionString);
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
                    throw new Exception("Type: {0} is not among the known types".FormatWith(type.FullName));
                }

                command.CommandText += " IN( '" + acceptableTypeNames.Join("','") + "')\n";
            }
        }

        private static void EnsureInitialized(string connectionString)
        {
            lock (LockObject)
            {
                if (!VerifiedTables.Contains(connectionString))
                {
                    using (var connection = OpenSession(connectionString))
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
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF) ON [PRIMARY]
) ON [PRIMARY]

CREATE NONCLUSTERED INDEX [IX_ValueType] ON [dbo].[Store] 
(
	[ValueType] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]

";
                                    createTableCommand.ExecuteNonQuery();
                                }
                            }
                            VerifiedTables.Add(connectionString);
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

        public static void PurgeDb(string connectionString)
        {
            lock (LockObject)
            {
                using (var connection = OpenSession(connectionString))
                {
                    using (var dropCommand = connection.CreateCommand())
                    {
                        dropCommand.CommandText =
                            @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Store]') AND type in (N'U'))
DROP TABLE [dbo].[Store]";

                        dropCommand.ExecuteNonQuery();

                        VerifiedTables.Remove(connectionString);
                        EnsureInitialized(connectionString);
                    }
                }
            }
        }
    }
}