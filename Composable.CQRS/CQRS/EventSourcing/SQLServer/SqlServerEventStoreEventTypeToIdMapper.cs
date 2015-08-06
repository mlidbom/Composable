using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using Composable.System.Reflection;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    internal class SqlServerEventStoreEventTypeToIdMapper
    {
        private static readonly ConcurrentDictionary<string, SqlServerEventStoreEventTypeToIdMapper> ConnectionStringToMapperDictionary = new ConcurrentDictionary<string, SqlServerEventStoreEventTypeToIdMapper>();
        public static SqlServerEventStoreEventTypeToIdMapper ForConnectionString(string connectionString)
        {
            return ConnectionStringToMapperDictionary.GetOrAdd(connectionString, key => new SqlServerEventStoreEventTypeToIdMapper(connectionString));
        }

        private readonly SqlServerEventStoreConnectionManager _connectionMananger;
        public SqlServerEventStoreEventTypeToIdMapper(string connectionMananger)
        {
            _connectionMananger = new SqlServerEventStoreConnectionManager(connectionMananger);            
        }

        public Type GetType(int id)
        {            
            lock(_lockObject)
            {
                EnsureInitialized();
                return _idToTypeMap[id];
            }
        }

        private void EnsureInitialized()
        {
            if(_idToTypeMap == null)
            {
                _idToTypeMap = new Dictionary<int, Type>();
                _typeToIdMap = new Dictionary<Type, int>();
                foreach (var mapping in GetTypes())
                {
                    _idToTypeMap.Add(mapping.Id, mapping.Type);
                    _typeToIdMap.Add(mapping.Type, mapping.Id);
                }
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
    

        private IdTypeMapping InsertNewType(Type newType)
        {          
            using (var connection = _connectionMananger.OpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"INSERT {EventTypeTable.Name} ( {EventTypeTable.Columns.EventType} ) OUTPUT INSERTED.{EventTypeTable.Columns.Id} VALUES( @{EventTypeTable.Columns.EventType} )";
                    command.Parameters.Add(new SqlParameter(EventTypeTable.Columns.EventType, newType.FullName));
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
                                type: reader.GetString(1).AsType());
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