using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Composable.DDD;
using Composable.GenericAbstractions.Time;
using Composable.Serialization;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Data.SqlClient;
using Composable.System.Linq;
using Composable.System.Reflection;

namespace Composable.Persistence.DocumentDb.SqlServer
{
    partial class SqlServerDocumentDb : IDocumentDb
    {
        readonly ISqlConnectionProvider _connectionProvider;
        readonly IUtcTimeTimeSource _timeSource;
        readonly IDocumentDbSerializer _serializer;

        const int UniqueConstraintViolationErrorNumber = 2627;

        readonly object _lockObject = new object();
        bool _initialized;
        ConcurrentDictionary<Type, int> _knownTypes = null;
        SchemaManager _schemaManager;

        internal SqlServerDocumentDb(ISqlConnectionProvider connectionProvider, IUtcTimeTimeSource timeSource, IDocumentDbSerializer serializer)
        {
            _schemaManager = new SqlServerDocumentDb.SchemaManager(connectionProvider);
            _connectionProvider = connectionProvider;
            _timeSource = timeSource;
            _serializer = serializer;
        }

        Type GetTypeFromId(int id) { return _knownTypes.Single(pair => pair.Value == id).Key; }

        bool IDocumentDb.TryGet<TValue>(object key, out TValue value, Dictionary<Type, Dictionary<string, string>> persistentValues)
        {
            EnsureInitialized();

            if(!IsKnownType(typeof(TValue)))
            {
                value = default(TValue);
                return false;
            }

            value = default(TValue);

            object found;
            using(var connection = _connectionProvider.OpenConnection())
            {
                using var command = connection.CreateCommand();
                var lockHint = DocumentDbSession.UseUpdateLock ? "With(UPDLOCK, ROWLOCK)" : "";
                command.CommandText = $@"
SELECT Value, ValueTypeId FROM Store {lockHint} 
WHERE Id=@Id AND ValueTypeId
";
                var idString = GetIdString(key);
                command.Parameters.Add(new SqlParameter("Id", SqlDbType.NVarChar, 500) {Value = idString});

                AddTypeCriteria(command, typeof(TValue));

                using var reader = command.ExecuteReader();
                if(!reader.Read())
                {
                    return false;
                }

                var stringValue = reader.GetString(0);
                found = _serializer.Deserialize(GetTypeFromId(reader.GetInt32(1)), stringValue);

                //Things such as TimeZone etc can cause roundtripping serialization to result in different values from the original so don't cache the read string. Cache the result of serializing it again.
                //performance: Try to find a way to remove the need to do this so that we can get rid of the overhead of an extra serialization.
                persistentValues.GetOrAddDefault(found.GetType())[idString] = _serializer.Serialize(found);
            }

            value = (TValue)found;
            return true;
        }

        public void Add<T>(object id, T value, Dictionary<Type, Dictionary<string, string>> persistentValues)
        {
            EnsureInitialized();

            var idString = GetIdString(id);
            EnsureTypeRegistered(value.GetType());
            using var connection = _connectionProvider.OpenConnection();
            using var command = connection.CreateCommand();
            var now = _timeSource.UtcNow;
            command.CommandType = CommandType.Text;

            command.CommandText += @"INSERT INTO Store(Id, ValueTypeId, Value, Created, Updated) VALUES(@Id, @ValueTypeId, @Value, @Created, @Updated)";

            command.Parameters.Add(new SqlParameter("Id", SqlDbType.NVarChar, 500) {Value = idString});
            command.Parameters.Add(new SqlParameter("ValueTypeId", SqlDbType.Int) {Value = _knownTypes[value.GetType()]});
            command.Parameters.Add(new SqlParameter("Created", SqlDbType.DateTime2) {Value = now});
            command.Parameters.Add(new SqlParameter("Updated", SqlDbType.DateTime2) {Value = now});

            var stringValue = _serializer.Serialize(value);
            command.Parameters.Add(new SqlParameter("Value", SqlDbType.NVarChar, -1) {Value = stringValue});

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

        static string GetIdString(object id) => id.ToString().ToLower().TrimEnd(' ');

        public void Remove<T>(object id)
        {
            EnsureInitialized();
            Remove(id, typeof(T));
        }
        public int RemoveAll<T>()
        {
            EnsureInitialized();
            using var connection = _connectionProvider.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText += "DELETE Store WHERE ValueTypeId = @TypeId";

            command.Parameters.Add(new SqlParameter("TypeId", SqlDbType.Int) {Value = _knownTypes[typeof(T)]});

            return command.ExecuteNonQuery();
        }

        public void Remove(object id, Type documentType)
        {
            EnsureInitialized();
            using var connection = _connectionProvider.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText += "DELETE Store WHERE Id = @Id AND ValueTypeId ";
            command.Parameters.Add(new SqlParameter("Id", SqlDbType.NVarChar, 500) {Value = GetIdString(id)});

            AddTypeCriteria(command, documentType);

            var rowsAffected = command.ExecuteNonQuery();
            if(rowsAffected < 1)
            {
                throw new NoSuchDocumentException(id, documentType);
            }

            if(rowsAffected > 1)
            {
                throw new TooManyItemsDeletedException();
            }
        }

        public void Update(IEnumerable<KeyValuePair<string, object>> values, Dictionary<Type, Dictionary<string, string>> persistentValues)
        {
            EnsureInitialized();
            values = values.ToList();
            using var connection = _connectionProvider.OpenConnection();
            foreach(var entry in values)
            {
                using var command = connection.CreateCommand();
                command.CommandType = CommandType.Text;
                var stringValue = _serializer.Serialize(entry.Value);

                var idString = GetIdString(entry.Key);
                var needsUpdate = !persistentValues.GetOrAddDefault(entry.Value.GetType()).TryGetValue(idString, out var oldValue) || stringValue != oldValue;
                if(needsUpdate)
                {
                    persistentValues.GetOrAddDefault(entry.Value.GetType())[idString] = stringValue;
                    command.CommandText += "UPDATE Store SET Value = @Value, Updated = @Updated WHERE Id = @Id AND ValueTypeId \n";
                    command.Parameters.Add(new SqlParameter("Id", SqlDbType.NVarChar, 500) {Value = entry.Key});
                    command.Parameters.Add(new SqlParameter("Updated", SqlDbType.DateTime2) {Value = _timeSource.UtcNow});

                    AddTypeCriteria(command, entry.Value.GetType());

                    command.Parameters.Add(new SqlParameter("Value", SqlDbType.NVarChar, -1) {Value = stringValue});
                }

                if(!command.CommandText.IsNullOrWhiteSpace())
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        IEnumerable<T> IDocumentDb.GetAll<T>()
        {
            EnsureInitialized();
            if(!IsKnownType(typeof(T)))
            {
                yield break;
            }

            using var connection = _connectionProvider.OpenConnection();
            using var loadCommand = connection.CreateCommand();
            loadCommand.CommandText = @" SELECT Id, Value, ValueTypeId FROM Store WHERE ValueTypeId ";

            AddTypeCriteria(loadCommand, typeof(T));

            using var reader = loadCommand.ExecuteReader();
            while(reader.Read())
            {
                yield return (T)_serializer.Deserialize(GetTypeFromId(reader.GetInt32(2)), reader.GetString(1));
            }
        }

        public IEnumerable<T> GetAll<T>(IEnumerable<Guid> ids) where T : IHasPersistentIdentity<Guid>
        {
            EnsureInitialized();
            if(!IsKnownType(typeof(T)))
            {
                yield break;
            }

            using var connection = _connectionProvider.OpenConnection();
            using var loadCommand = connection.CreateCommand();
            loadCommand.CommandText = @"SELECT Id, Value, ValueTypeId FROM Store WHERE ValueTypeId ";

            AddTypeCriteria(loadCommand, typeof(T));

            loadCommand.CommandText += " AND Id IN('" + ids.Select(id => id.ToString()).Join("','") + "')";

            using var reader = loadCommand.ExecuteReader();
            while(reader.Read())
            {
                yield return (T)_serializer.Deserialize(GetTypeFromId(reader.GetInt32(2)), reader.GetString(1));
            }
        }

        public IEnumerable<Guid> GetAllIds<T>() where T : IHasPersistentIdentity<Guid>
        {
            EnsureInitialized();
            if(!IsKnownType(typeof(T)))
            {
                yield break;
            }

            using var connection = _connectionProvider.OpenConnection();
            using var loadCommand = connection.CreateCommand();
            loadCommand.CommandText = @"SELECT Id FROM Store WHERE ValueTypeId ";

            AddTypeCriteria(loadCommand, typeof(T));

            using var reader = loadCommand.ExecuteReader();
            while(reader.Read())
            {
                yield return Guid.Parse(reader.GetString(0));
            }
        }

        void EnsureTypeRegistered(Type type)
        {
            lock(_lockObject)
            {
                if(!IsKnownType(type))
                {
                    using var connection = _connectionProvider.OpenConnection();
                    using var command = connection.CreateCommand();
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
                    command.Parameters.Add(new SqlParameter("ValueType", SqlDbType.VarChar, 500) {Value = type.FullName});
                    command.ExecuteNonQuery();
                    _knownTypes.TryAdd(type, (int)command.Parameters["ValueTypeId"].Value);
                }
            }
        }

        bool IsKnownType(Type type)
        {
            if(!_knownTypes.ContainsKey(type))
            {
                RefreshKnownTypes();
            }

            return _knownTypes.ContainsKey(type);
        }

        void AddTypeCriteria(SqlCommand command, Type type)
        {
            lock(_lockObject)
            {
                var acceptableTypeIds = _knownTypes.Where(x => type.IsAssignableFrom(x.Key)).Select(t => t.Value.ToString()).ToArray();
                if(acceptableTypeIds.None())
                {
                    throw new Exception($"Type: {type.FullName} is not among the known types");
                }

                command.CommandText += "IN( " + acceptableTypeIds.Join(",") + ")\n";
            }
        }

        void EnsureInitialized()
        {
            lock(_lockObject)
            {
                if(!_initialized)
                {
                    _schemaManager.EnsureInitialized();

                    _knownTypes = new ConcurrentDictionary<Type, int>();
                    _initialized = true;

                    RefreshKnownTypes();
                }
            }
        }

        void RefreshKnownTypes()
        {
            lock(_lockObject)
            {
                _connectionProvider.ExecuteReader("SELECT DISTINCT ValueType, Id FROM ValueType",
                                         reader => _knownTypes.TryAdd(reader.GetString(0).AsType(), reader.GetInt32(1)));
            }
        }
    }
}
