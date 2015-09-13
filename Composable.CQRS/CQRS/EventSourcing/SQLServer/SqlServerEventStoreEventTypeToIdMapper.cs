using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using Composable.CQRS.EventSourcing.EventRefactoring;
using Composable.System.Reflection;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    internal class SqlServerEventStoreEventTypeToIdMapper
    {
        private readonly IEventNameMapper _nameMapper;
        private static readonly ConcurrentDictionary<string, SqlServerEventStoreEventTypeToIdMapper> ConnectionStringToMapperDictionary = new ConcurrentDictionary<string, SqlServerEventStoreEventTypeToIdMapper>();
        public static SqlServerEventStoreEventTypeToIdMapper ForConnectionString(string connectionString, IEventNameMapper nameMapper)
        {
            return ConnectionStringToMapperDictionary.GetOrAdd(connectionString, key => new SqlServerEventStoreEventTypeToIdMapper(connectionString, nameMapper));
        }

        private readonly SqlServerEventStoreConnectionManager _connectionMananger;
        public SqlServerEventStoreEventTypeToIdMapper(string connectionString, IEventNameMapper nameMapper)
        {
            _nameMapper = nameMapper;
            _connectionMananger = new SqlServerEventStoreConnectionManager(connectionString);
        }

        public Type GetType(int id)
        {            
            lock(_lockObject)
            {
                EnsureInitialized();
                return _idToTypeMap[id];
            }
        }        

        public int GetId(Type type)
        {
            lock(_lockObject)
            {
                EnsureInitialized();
                int value;
                if(!_typeToIdMap.TryGetValue(type, out value))
                {
                    var mapping = InsertNewType(type);
                    _idToTypeMap.Add(mapping.Id, mapping.Type);
                    _typeToIdMap.Add(mapping.Type, mapping.Id);
                    value = mapping.Id;
                }
                return value;
            }
        }

        private void EnsureInitialized()
        {
            if (_idToTypeMap == null)
            {
                var idToTypeMap = new Dictionary<int, Type>();
                var typeToIdMap = new Dictionary<Type, int>();
                foreach (var mapping in GetTypes())
                {
                    idToTypeMap.Add(mapping.Id, mapping.Type);
                    typeToIdMap.Add(mapping.Type, mapping.Id);
                }
                _idToTypeMap = idToTypeMap;//Only assign to the fields once we completely and successfully fetch all types. We do not want a half-way populated, and therefore corrupt, mapping table.
                _typeToIdMap = typeToIdMap;
            }
        }


        private IdTypeMapping InsertNewType(Type newType)
        {          
            using (var connection = _connectionMananger.OpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"INSERT {EventTypeTable.Name} ( {EventTypeTable.Columns.EventType} ) OUTPUT INSERTED.{EventTypeTable.Columns.Id} VALUES( @{EventTypeTable.Columns.EventType} )";
                    command.Parameters.Add(new SqlParameter(EventTypeTable.Columns.EventType, _nameMapper.GetName(newType)));
                    return new IdTypeMapping(id: (int)command.ExecuteScalar(), type: newType);
                }
            }
        }       

        private IEnumerable<IdTypeMapping> GetTypes()
        {
            using(var connection = _connectionMananger.OpenConnection())
            {
                using(var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT {EventTypeTable.Columns.Id} , {EventTypeTable.Columns.EventType} FROM {EventTypeTable.Name}";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return new IdTypeMapping(
                                id: reader.GetInt32(0),
                                type: _nameMapper.GetType(reader.GetString(1)));
                        }
                    }
                }
            }
        }


        private Dictionary<int, Type> _idToTypeMap;
        private Dictionary<Type, int> _typeToIdMap;
        private readonly object _lockObject = new object();        

        private class IdTypeMapping
        {
            public int Id { get; }
            public Type Type { get; }
            public IdTypeMapping(int id, Type type)
            {
                Id = id;
                Type = type;
            }
        }
    }
}