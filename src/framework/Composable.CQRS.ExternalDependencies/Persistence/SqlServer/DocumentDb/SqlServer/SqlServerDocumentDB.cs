using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Composable.Contracts;
using Composable.DDD;
using Composable.GenericAbstractions.Time;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System;
using Composable.System.Collections.Collections;

namespace Composable.Persistence.SqlServer.DocumentDb.SqlServer
{
    //urgent: Extract persistence from this class to an interface called IDocumentDbPersistenceLayer then rename this class DocumentDb
    partial class SqlServerDocumentDb : IDocumentDb
    {
        readonly ISqlServerConnectionProvider _connectionProvider;
        readonly IUtcTimeTimeSource _timeSource;
        readonly IDocumentDbSerializer _serializer;

        const int UniqueConstraintViolationErrorNumber = 2627;

        readonly object _lockObject = new object();
        bool _initialized;
        readonly ITypeMapper _typeMapper;
        readonly SchemaManager _schemaManager;

        internal SqlServerDocumentDb(ISqlServerConnectionProvider connectionProvider, IUtcTimeTimeSource timeSource, IDocumentDbSerializer serializer, ITypeMapper typeMapper)
        {
            _schemaManager = new SqlServerDocumentDb.SchemaManager(connectionProvider);
            _connectionProvider = connectionProvider;
            _timeSource = timeSource;
            _serializer = serializer;
            _typeMapper = typeMapper;
        }

        Type GetTypeFromId(TypeId id) { return _typeMapper.GetType(id); }

        bool IDocumentDb.TryGet<TValue>(object key, [NotNullWhen(true)][MaybeNull]out TValue value, Dictionary<Type, Dictionary<string, string>> persistentValues)
        {
            EnsureInitialized();

            value = default;

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

                Storage_AddTypeCriteria(command, typeof(TValue));

                using var reader = command.ExecuteReader();
                if(!reader.Read())
                {
                    return false;
                }

                var stringValue = reader.GetString(0);
                found = _serializer.Deserialize(GetTypeFromId(new TypeId(reader.GetGuid(1))), stringValue);

                //Things such as TimeZone etc can cause roundtripping serialization to result in different values from the original so don't cache the read string. Cache the result of serializing it again.
                //performance: Try to find a way to remove the need to do this so that we can get rid of the overhead of an extra serialization.
                persistentValues.GetOrAddDefault(found.GetType())[idString] = _serializer.Serialize(found);
            }

            value = (TValue)found;
            return true;
        }

        public void Add<T>(object id, T value, Dictionary<Type, Dictionary<string, string>> persistentValues)
        {
            Assert.Argument.NotNull(value);
            EnsureInitialized();

            var idString = GetIdString(id);
            using var connection = _connectionProvider.OpenConnection();
            using var command = connection.CreateCommand();
            var now = _timeSource.UtcNow;
            command.CommandType = CommandType.Text;

            command.CommandText += @"INSERT INTO Store(Id, ValueTypeId, Value, Created, Updated) VALUES(@Id, @ValueTypeId, @Value, @Created, @Updated)";

            command.Parameters.Add(new SqlParameter("Id", SqlDbType.NVarChar, 500) {Value = idString});
            command.Parameters.Add(new SqlParameter("ValueTypeId", SqlDbType.UniqueIdentifier) {Value = _typeMapper.GetId(value.GetType()).GuidValue});
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

            command.Parameters.Add(new SqlParameter("TypeId", SqlDbType.UniqueIdentifier) {Value = _typeMapper.GetId(typeof(T)).GuidValue});

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

            Storage_AddTypeCriteria(command, documentType);

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

            var toUpdate = new List<(string id, string idString, string stringValue,Type type)>();
            foreach(var item in values)
            {
                var stringValue = _serializer.Serialize(item.Value);
                var idString = GetIdString(item.Key);
                var needsUpdate = !persistentValues.GetOrAddDefault(item.Value.GetType()).TryGetValue(idString, out var oldValue) || stringValue != oldValue;
                if(needsUpdate)
                {
                    toUpdate.Add((item.Key, idString, stringValue, item.Value.GetType()));
                }
            }

            Storage_Update(persistentValues, toUpdate);
        }

        void Storage_Update(Dictionary<Type, Dictionary<string, string>> persistentValues, List<(string id, string idString, string stringValue, Type type)> toUpdate)
        {
            using var connection = _connectionProvider.OpenConnection();
            foreach(var (id, idString, stringValue, type) in toUpdate)
            {
                using var command = connection.CreateCommand();
                command.CommandType = CommandType.Text;
                persistentValues.GetOrAddDefault(type)[idString] = stringValue;
                command.CommandText += "UPDATE Store SET Value = @Value, Updated = @Updated WHERE Id = @Id AND ValueTypeId \n";
                command.Parameters.Add(new SqlParameter("Id", SqlDbType.NVarChar, 500) {Value = id});
                command.Parameters.Add(new SqlParameter("Updated", SqlDbType.DateTime2) {Value = _timeSource.UtcNow});

                Storage_AddTypeCriteria(command, type);

                command.Parameters.Add(new SqlParameter("Value", SqlDbType.NVarChar, -1) {Value = stringValue});
                command.ExecuteNonQuery();
            }
        }

        IEnumerable<T> IDocumentDb.GetAll<T>()
        {
            EnsureInitialized();

            using var connection = _connectionProvider.OpenConnection();
            using var loadCommand = connection.CreateCommand();
            loadCommand.CommandText = @" SELECT Id, Value, ValueTypeId FROM Store WHERE ValueTypeId ";

            Storage_AddTypeCriteria(loadCommand, typeof(T));

            using var reader = loadCommand.ExecuteReader();
            while(reader.Read())
            {
                yield return (T)_serializer.Deserialize(GetTypeFromId(new TypeId(reader.GetGuid(2))), reader.GetString(1));
            }
        }

        public IEnumerable<T> GetAll<T>(IEnumerable<Guid> ids) where T : IHasPersistentIdentity<Guid>
        {
            EnsureInitialized();

            using var connection = _connectionProvider.OpenConnection();
            using var loadCommand = connection.CreateCommand();
            loadCommand.CommandText = @"SELECT Id, Value, ValueTypeId FROM Store WHERE ValueTypeId ";

            Storage_AddTypeCriteria(loadCommand, typeof(T));

            loadCommand.CommandText += " AND Id IN('" + ids.Select(id => id.ToString()).Join("','") + "')";

            using var reader = loadCommand.ExecuteReader();
            while(reader.Read())
            {
                yield return (T)_serializer.Deserialize(GetTypeFromId(new TypeId(reader.GetGuid(2))), reader.GetString(1));
            }
        }

        public IEnumerable<Guid> GetAllIds<T>() where T : IHasPersistentIdentity<Guid>
        {
            EnsureInitialized();

            using var connection = _connectionProvider.OpenConnection();
            using var loadCommand = connection.CreateCommand();
            loadCommand.CommandText = @"SELECT Id FROM Store WHERE ValueTypeId ";

            Storage_AddTypeCriteria(loadCommand, typeof(T));

            using var reader = loadCommand.ExecuteReader();
            while(reader.Read())
            {
                yield return Guid.Parse(reader.GetString(0));
            }
        }

        void Storage_AddTypeCriteria(SqlCommand command, Type type)
        {
            lock(_lockObject)
            {
                command.CommandText += "IN( '" + _typeMapper.GetIdForTypesAssignableTo(type).Select(@this => @this.GuidValue.ToString()).Join("', '") + "')\n";
            }
        }

        void EnsureInitialized()
        {
            lock(_lockObject)
            {
                if(!_initialized)
                {
                    _schemaManager.EnsureInitialized();
                    _initialized = true;
                }
            }
        }
    }
}
