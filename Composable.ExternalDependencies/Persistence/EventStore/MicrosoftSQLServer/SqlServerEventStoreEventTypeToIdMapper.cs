using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Composable.Logging.Log4Net;
using Composable.Persistence.EventStore.Refactoring.Naming;

namespace Composable.Persistence.EventStore.MicrosoftSQLServer
{
    class SqlServerEventStoreEventTypeToIdMapper : IEventTypeToIdMapper
    {
        readonly IEventNameMapper _nameMapper;

        readonly SqlServerEventStoreConnectionManager _connectionMananger;
        public SqlServerEventStoreEventTypeToIdMapper(Lazy<string> connectionString, IEventNameMapper nameMapper)
        {
            _nameMapper = nameMapper;
            _connectionMananger = new SqlServerEventStoreConnectionManager(connectionString);
        }

        public Type GetType(int id)
        {
            lock(_lockObject)
            {
                EnsureInitialized();
                IIdTypeMapping result;
                if(_idToTypeMap.TryGetValue(id, out result))
                {
                    return result.Type;
                }

                LoadTypesFromDatabase();

                if (!_idToTypeMap.TryGetValue(id, out result))
                {
                    throw new Exception($"Failed to load type information Id: {id} from the eventstore");
                }

                return result.Type;
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
                    _idToTypeMap.Add(mapping.Id, mapping);
                    _typeToIdMap.Add(mapping.Type, mapping.Id);
                    value = mapping.Id;
                }
                return value;
            }
        }

        void EnsureInitialized()
        {
            if (_idToTypeMap == null)
            {
                LoadTypesFromDatabase();
            }
        }

        public void LoadTypesFromDatabase()
        {
            lock(_lockObject)
            {
                var idToTypeMap = new Dictionary<int, IIdTypeMapping>();
                var typeToIdMap = new Dictionary<Type, int>();
                foreach(var mapping in GetTypes())
                {
                    idToTypeMap.Add(mapping.Id, mapping);
                    if(!(mapping is BrokenIdTypeMapping))
                    {
                        typeToIdMap.Add(mapping.Type, mapping.Id);
                    }
                }
                _idToTypeMap = idToTypeMap;
                //Only assign to the fields once we completely and successfully fetch all types. We do not want a half-way populated, and therefore corrupt, mapping table.
                _typeToIdMap = typeToIdMap;
            }
        }

        IdTypeMapping InsertNewType(Type newType)
        {
            using (var connection = _connectionMananger.OpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"SELECT {EventTypeTable.Columns.Id} FROM {EventTypeTable.Name} WHERE {EventTypeTable.Columns.EventType}=@{EventTypeTable.Columns.EventType}";
                    command.Parameters.Add(new SqlParameter(EventTypeTable.Columns.EventType, SqlDbType.NVarChar, 450) {Value = _nameMapper.GetName(newType)});
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new IdTypeMapping(id: reader.GetInt32(0), type: newType);
                        }
                    }
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"INSERT {EventTypeTable.Name} ( {EventTypeTable.Columns.EventType} ) OUTPUT INSERTED.{EventTypeTable.Columns.Id} VALUES( @{EventTypeTable.Columns.EventType} )";
                    command.Parameters.Add(new SqlParameter(EventTypeTable.Columns.EventType, SqlDbType.NVarChar, 450) {Value = _nameMapper.GetName(newType)});
                    return new IdTypeMapping(id: (int)command.ExecuteScalar(), type: newType);
                }
            }
        }

        IEnumerable<IIdTypeMapping> GetTypes()
        {
            using(var connection = _connectionMananger.OpenConnection(suppressTransactionWarning:true))
            {
                using(var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT {EventTypeTable.Columns.Id} , {EventTypeTable.Columns.EventType} FROM {EventTypeTable.Name}";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var eventTypeName = reader.GetString(1);
                            var eventTypeId = reader.GetInt32(0);
                            Type foundEventType = null;

                            try
                            {
                                foundEventType = _nameMapper.GetType(eventTypeName);
                            }
                            catch (CouldNotFindTypeBasedOnName)
                            {
                                this.Log().Warn($"The type of event: Id: {eventTypeId}, Name: {eventTypeName} that exists in the database could not be found in the loaded assemblies. No mapping will be created for this class. If an event of this type is read from the store an exception will be thrown");
                            }

                            if(foundEventType != null)
                            {
                                yield return new IdTypeMapping(id: eventTypeId, type: foundEventType);
                            }
                            else
                            {
                                yield return new BrokenIdTypeMapping(id: eventTypeId, typeName: eventTypeName);
                            }
                        }
                    }
                }
            }
        }

        Dictionary<int, IIdTypeMapping> _idToTypeMap;
        Dictionary<Type, int> _typeToIdMap;
        readonly object _lockObject = new object();

        interface IIdTypeMapping {
            int Id { get; }
            Type Type { get; }
        }

        class BrokenIdTypeMapping : IIdTypeMapping
        {
            readonly string _typeName;
            public BrokenIdTypeMapping(int id, string typeName)
            {
                _typeName = typeName;
                Id = id;
            }
            public int Id { get; }
            public Type Type => throw new TryingToReadEventOfTypeThatNoMappingCouldBeFoundForException(_typeName, Id);
        }

        class IdTypeMapping : IIdTypeMapping
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

    class TryingToReadEventOfTypeThatNoMappingCouldBeFoundForException : Exception
    {
        public TryingToReadEventOfTypeThatNoMappingCouldBeFoundForException(string typeName, int id):base($"Event type Id: {id}, Name: {typeName} could not be mapped to a type.") {  }
    }
}