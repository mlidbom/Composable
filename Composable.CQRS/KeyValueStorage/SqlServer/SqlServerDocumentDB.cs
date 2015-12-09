using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Composable.DDD;
using Composable.NewtonSoft;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using Composable.System.Reactive;
using Composable.System.Reflection;
using Newtonsoft.Json;
using log4net;

namespace Composable.KeyValueStorage.SqlServer
{
    public class SqlServerDocumentDb : IDocumentDb
    {
        public readonly string ConnectionString;

        private static readonly JsonSerializerSettings _jsonSettings = JsonSettings.JsonSerializerSettings;

        private const int UniqueConstraintViolationErrorNumber = 2627;

        private static readonly object StaticLockObject = new object();

        public SqlServerDocumentDb(string connectionString)
        {
            ConnectionString = connectionString;
        }

        private readonly ThreadSafeObservable<IDocumentUpdated> _documentUpdated = new ThreadSafeObservable<IDocumentUpdated>(); 
        public IObservable<IDocumentUpdated> DocumentUpdated { get { return _documentUpdated; } }

        public ConcurrentDictionary<Type, int> KnownTypes { get { return VerifiedConnections[ConnectionString]; } }

        private Type GetTypeFromId(int id)
        {
            return KnownTypes.Single(pair => pair.Value == id).Key;
        }

        public bool TryGet<TValue>(object key, out TValue value, Dictionary<Type, Dictionary<string, string>> persistentValues)
        {           
            EnsureInitialized();

            if (!IsKnownType(typeof(TValue)))
            {
                value = default(TValue);
                return false;
            }

            value = default(TValue);

            object found;
            using(var connection = OpenSession())
            {
                using(var command = connection.CreateCommand())
                {
                    string lockHint = DocumentDbSession.UseUpdateLock ? "With(UPDLOCK, ROWLOCK)" : "";
                    command.CommandText = @"
SELECT Value, ValueTypeId FROM Store {0} 
WHERE Id=@Id AND ValueTypeId
".FormatWith(lockHint);
                    string idString = GetIdString(key);
                    command.Parameters.Add(new SqlParameter("Id", idString));

                    AddTypeCriteria(command, typeof(TValue));

                    using(var reader = command.ExecuteReader())
                    {
                        if(!reader.Read())
                        {
                            return false;
                        }
                        var stringValue = reader.GetString(0);
                        found = JsonConvert.DeserializeObject(stringValue, GetTypeFromId(reader.GetInt32(1)), _jsonSettings);

                        //Things such as TimeZone etc can cause roundtripping serialization to result in different values from the original so don't cache the read string. Cache the result of serializing it again.
                        //Todo: Try to find a way to remove the need to do this so that we can get rid of the overhead of an extra serialization.
                        persistentValues.GetOrAddDefault(found.GetType())[idString] = JsonConvert.SerializeObject(found, Formatting.None, _jsonSettings);
                    }
                }
            }

            value = (TValue)found;
            return true;
        }

        public void Add<T>(object id, T value, Dictionary<Type, Dictionary<string, string>> persistentValues)
        {
            EnsureInitialized();

            string idString = GetIdString(id);
            EnsureTypeRegistered(value.GetType());
            using(var connection = OpenSession())
            {
                using(var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;

                    command.CommandText += @"INSERT INTO Store(Id, ValueTypeId, Value) VALUES(@Id, @ValueTypeId, @Value)";

                    command.Parameters.Add(new SqlParameter("Id", idString));
                    command.Parameters.Add(new SqlParameter("ValueTypeId", KnownTypes[value.GetType()]));

                    var stringValue = JsonConvert.SerializeObject(value, Formatting.None, _jsonSettings);
                    command.Parameters.Add(new SqlParameter("Value", stringValue));

                    _documentUpdated.OnNext(new DocumentUpdated(idString, value));

                    persistentValues.GetOrAddDefault(value.GetType())[idString] = stringValue;
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch(SqlException e)
                    {
                        if(e.Number == UniqueConstraintViolationErrorNumber)
                        {
                            throw new AttemptToSaveAlreadyPersistedValueException(id, value);
                        }
                        throw;
                    }
                }
            }
        }

        private static string GetIdString(object id)
        {
            return id.ToString().ToLower().TrimEnd(' ');
        }

        public bool Remove<T>(object id)
        {
            EnsureInitialized();
            return Remove(id, typeof(T));
        }
        public int RemoveAll<T>()
        {
            EnsureInitialized();
            using (var connection = OpenSession())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText += "DELETE Store WHERE ValueTypeId = @TypeId";

                    command.Parameters.Add(new SqlParameter("TypeId", KnownTypes[typeof(T)]));

                    return command.ExecuteNonQuery();
                }
            }
        }

        public bool Remove(object id, Type documentType)
        {
            EnsureInitialized();
            using(var connection = OpenSession())
            {
                using(var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText += "DELETE Store WHERE Id = @Id AND ValueTypeId ";
                    command.Parameters.Add(new SqlParameter("Id", GetIdString(id)));

                    AddTypeCriteria(command, documentType);

                    var rowsAffected = command.ExecuteNonQuery();
                    if(rowsAffected > 1)
                    {
                        throw new TooManyItemsDeletedException();
                    }
                    return rowsAffected > 0;
                }
            }
        }

        public void Update(IEnumerable<KeyValuePair<string, object>> values, Dictionary<Type, Dictionary<string, string>> persistentValues)
        {
            EnsureInitialized();
            values = values.ToList();
            using(var connection = OpenSession())
            {
                foreach(var entry in values)
                {
                    using(var command = connection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;
                        var stringValue = JsonConvert.SerializeObject(entry.Value, Formatting.None, _jsonSettings);

                        string oldValue;
                        string idString = GetIdString(entry.Key);
                        var needsUpdate = !persistentValues.GetOrAddDefault(entry.Value.GetType()).TryGetValue(idString, out oldValue) || stringValue != oldValue;
                        if(needsUpdate)
                        {
                            persistentValues.GetOrAddDefault(entry.Value.GetType())[idString] = stringValue;
                            command.CommandText += "UPDATE Store SET Value = @Value WHERE Id = @Id AND ValueTypeId \n";
                            command.Parameters.Add(new SqlParameter("Id", entry.Key));

                            AddTypeCriteria(command, entry.Value.GetType());

                            command.Parameters.Add(new SqlParameter("Value", stringValue));
                        }
                        if(!command.CommandText.IsNullOrWhiteSpace())
                        {
                            command.ExecuteNonQuery();
                            _documentUpdated.OnNext(new DocumentUpdated(entry.Key, entry.Value));
                        }
                    }
                }
            }
        }


        IEnumerable<T> IDocumentDb.GetAll<T>()
        {
            EnsureInitialized();
            if (!IsKnownType(typeof(T)))
            {
                yield break;
            }

            using(var connection = OpenSession())
            {
                using(var loadCommand = connection.CreateCommand())
                {
                    loadCommand.CommandText = @"
SELECT Id, Value, ValueTypeId 
FROM Store 
WHERE ValueTypeId ";

                    AddTypeCriteria(loadCommand, typeof(T));

                    using(var reader = loadCommand.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            yield return (T)JsonConvert.DeserializeObject(reader.GetString(1), GetTypeFromId(reader.GetInt32(2)), _jsonSettings);
                        }
                    }
                }
            }
        }

        public IEnumerable<T> GetAll<T>(IEnumerable<Guid> ids) where T : IHasPersistentIdentity<Guid>
        {
            EnsureInitialized();
            if (!IsKnownType(typeof(T)))
            {
                yield break;
            }

            using (var connection = OpenSession())
            {
                using (var loadCommand = connection.CreateCommand())
                {
                    loadCommand.CommandText = @"
SELECT Id, Value, ValueTypeId 
FROM Store 
WHERE ValueTypeId ";

                    AddTypeCriteria(loadCommand, typeof(T));

                    loadCommand.CommandText += " AND Id IN('" + ids.Select(id => id.ToString()).Join("','") + "')";

                    using (var reader = loadCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return (T)JsonConvert.DeserializeObject(reader.GetString(1), GetTypeFromId(reader.GetInt32(2)), _jsonSettings);
                        }
                    }
                }
            }
        }

        public IEnumerable<Guid> GetAllIds<T>() where T : IHasPersistentIdentity<Guid>
        {
            EnsureInitialized();
            if (!IsKnownType(typeof(T)))
            {
                yield break;
            }

            using (var connection = OpenSession())
            {
                using (var loadCommand = connection.CreateCommand())
                {
                    loadCommand.CommandText = @"
SELECT Id 
FROM Store 
WHERE ValueTypeId ";

                    AddTypeCriteria(loadCommand, typeof(T));

                    using (var reader = loadCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return Guid.Parse(reader.GetString(0));
                        }
                    }
                }
            }
        }

        private bool _disposed;

        public void Dispose()
        {
            if(!_disposed)
            {
                _disposed = true;
            }
        }

        private SqlConnection OpenSession()
        {
            return OpenSession(ConnectionString);
        }

        private static SqlConnection OpenSession(string connectionString)
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        private void EnsureTypeRegistered(Type type)
        {
            lock(StaticLockObject)
            {
                if(!IsKnownType(type))
                {
                    using(var connection = OpenSession())
                    {
                        using(var command = connection.CreateCommand())
                        {
                            command.CommandText = @"

IF NOT EXISTS(SELECT Id FROM ValueType WHERE ValueType = @ValueType)
	BEGIN
		INSERT INTO ValueType(ValueType)Values(@ValueType)
		SET @ValueTypeId = @@IDENTITY
	END
ELSE
	BEGIN
		SELECT @ValueTypeId = Id FROM ValueType WHERE ValueType = @ValueType
	END
";
                            command.Parameters.Add(new SqlParameter("ValueTypeId", SqlDbType.Int) {Direction = ParameterDirection.Output});
                            command.Parameters.Add(new SqlParameter("ValueType", type.FullName));
                            command.ExecuteNonQuery();
                            KnownTypes.TryAdd(type, (int)command.Parameters["ValueTypeId"].Value);
                        }
                    }
                }
            }
        }

        private bool IsKnownType(Type type)
        {
            if(!KnownTypes.ContainsKey(type))
            {
                RefreshKnownTypes(ConnectionString, KnownTypes);
            }
            return KnownTypes.ContainsKey(type);
        }

        private void AddTypeCriteria(SqlCommand command, Type type)
        {
            lock(StaticLockObject)
            {
                var acceptableTypeIds = KnownTypes.Where(x => type.IsAssignableFrom(x.Key)).Select(t => t.Value.ToString()).ToArray();
                if(acceptableTypeIds.None())
                {
                    throw new Exception("Type: {0} is not among the known types".FormatWith(type.FullName));
                }
                command.CommandText += "IN( " + acceptableTypeIds.Join(",") + ")\n";
            }
        }

        private void EnsureInitialized()
        {
            EnsureInitialized(ConnectionString);
        }

        private static void EnsureInitialized(string connectionString)
        {
            lock(StaticLockObject)
            {
                if(!VerifiedConnections.ContainsKey(connectionString))
                {
                    using(var connection = OpenSession(connectionString))
                    {
                        using(var checkForValueTypeCommand = connection.CreateCommand())
                        {
                            checkForValueTypeCommand.CommandText = "select count(*) from sys.tables where name = 'ValueType'";
                            var valueTypeExists = (int)checkForValueTypeCommand.ExecuteScalar();
                            if(valueTypeExists == 0)
                            {
                                using(var createValueTypeCommand = connection.CreateCommand())
                                {
                                    createValueTypeCommand.CommandText = @"
CREATE TABLE [dbo].[ValueType](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ValueType] [varchar](500) NOT NULL,
 CONSTRAINT [PK_ValueType] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
";
                                    createValueTypeCommand.ExecuteNonQuery();
                                }
                            }
                        }
                        using(var checkForStoreCommand = connection.CreateCommand())
                        {
                            checkForStoreCommand.CommandText = "select count(*) from sys.tables where name = 'Store'";
                            var exists = (int)checkForStoreCommand.ExecuteScalar();
                            if(exists == 0)
                            {
                                using(var createStoreCommand = connection.CreateCommand())
                                {
                                    createStoreCommand.CommandText =
                                        @"
CREATE TABLE [dbo].[Store](
	[Id] [nvarchar](500) NOT NULL,
	[ValueTypeId] [int] NOT NULL,
	[Value] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_Store] PRIMARY KEY CLUSTERED 
(
	[Id] ASC,
	[ValueTypeId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[Store]  WITH CHECK ADD  CONSTRAINT [FK_ValueType_Store] FOREIGN KEY([ValueTypeId])
REFERENCES [dbo].[ValueType] ([Id])

ALTER TABLE [dbo].[Store] CHECK CONSTRAINT [FK_ValueType_Store]

";
                                    createStoreCommand.ExecuteNonQuery();
                                }
                            }
                        }

                        var knownTypes = new ConcurrentDictionary<Type, int>();
                        VerifiedConnections.TryAdd(connectionString, knownTypes);

                        RefreshKnownTypes(connectionString, knownTypes);
                    }
                }
            }
        }

        private static void RefreshKnownTypes(String connectionString, ConcurrentDictionary<Type, int> knownTypes)
        {
            lock(StaticLockObject)
            {
                using(var connection = OpenSession(connectionString))
                {
                    using(var findTypesCommand = connection.CreateCommand())
                    {
                        findTypesCommand.CommandText = "SELECT DISTINCT ValueType, Id FROM ValueType";
                        using(var reader = findTypesCommand.ExecuteReader())
                        {
                            while(reader.Read())
                            {
                                knownTypes.TryAdd(reader.GetString(0).AsType(), reader.GetInt32(1));
                            }
                        }
                    }
                }
            }
        }


        private static readonly ConcurrentDictionary<String, ConcurrentDictionary<Type, int>> VerifiedConnections = new ConcurrentDictionary<string, ConcurrentDictionary<Type, int>>();

        public static void ResetDB(string connectionString)
        {
            lock (StaticLockObject)
            {
                using (var connection = OpenSession(connectionString))
                {
                    using (var dropCommand = connection.CreateCommand())
                    {
                        dropCommand.CommandText =
                            @"
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Store]') AND type in (N'U'))
DROP TABLE [dbo].[Store];
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ValueType]') AND type in (N'U'))
DROP TABLE [dbo].[ValueType];
";

                        dropCommand.ExecuteNonQuery();

                        ConcurrentDictionary<Type, int> ignored;
                        VerifiedConnections.TryRemove(connectionString, out ignored);
                        EnsureInitialized(connectionString);
                    }
                }
            }
        }
    }
}
