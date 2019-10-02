using System;
using System.Collections.Generic;
using Composable.Logging;
using Composable.Refactoring.Naming;
using Composable.System.Data.SqlClient;
using Composable.System.Transactions;

namespace Composable.Persistence.EventStore.MicrosoftSQLServer
{
    class SqlServerEventStoreEventTypeToIdMapper : IEventTypeToIdMapper
    {
        readonly ITypeMapper _typeMapper;

        readonly SqlServerEventStoreConnectionManager _connectionManager;
        public SqlServerEventStoreEventTypeToIdMapper(ISqlConnectionProvider connectionProvider, ITypeMapper typeMapper)
        {
            _typeMapper = typeMapper;
            _connectionManager = new SqlServerEventStoreConnectionManager(connectionProvider);
        }

        public Type GetType(int id)
        {
            lock(_lockObject)
            {
                EnsureInitialized();
                if(_idToTypeMap.TryGetValue(id, out var result))
                {
                    return result.Type;
                }

                LoadTypesFromDatabase();

                if(!_idToTypeMap.TryGetValue(id, out result))
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
                if(!_typeToIdMap.TryGetValue(type, out var value))
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
            if(_idToTypeMap == null)
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

        IdTypeMapping InsertNewType(Type newType) => TransactionScopeCe.SuppressAmbientAndExecuteInNewTransaction(() =>
        {
            using var connection = _connectionManager.OpenConnection(suppressTransactionWarning: true);
            using(var command = connection.CreateCommand())
            {
                var reader = command.SetCommandText($@"SELECT {EventTypeTable.Columns.Id} FROM {EventTypeTable.Name} WHERE {EventTypeTable.Columns.EventType}=@{EventTypeTable.Columns.EventType}")
                                    .AddParameter(EventTypeTable.Columns.EventType, _typeMapper.GetId(newType).GuidValue)
                                    .ExecuteReader();
                using(reader)
                {
                    if(reader.Read())
                    {
                        return new IdTypeMapping(id: reader.GetInt32(0), type: newType);
                    }
                }
            }

            using(var command = connection.CreateCommand())
            {
                var insertedTypeIntegerId = command.SetCommandText($@"INSERT {EventTypeTable.Name} ( {EventTypeTable.Columns.EventType} ) OUTPUT INSERTED.{EventTypeTable.Columns.Id} VALUES( @{EventTypeTable.Columns.EventType} )")
                                                   .AddParameter(EventTypeTable.Columns.EventType, _typeMapper.GetId(newType).GuidValue)
                                                   .ExecuteScalar();
                return new IdTypeMapping(id: (int)insertedTypeIntegerId, type: newType);
            }
        });

        IEnumerable<IIdTypeMapping> GetTypes() => TransactionScopeCe.SuppressAmbient(() =>
        {
            var types = new List<IIdTypeMapping>();
            using(var connection = _connectionManager.OpenConnection(suppressTransactionWarning: true))
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT {EventTypeTable.Columns.Id} , {EventTypeTable.Columns.EventType} FROM {EventTypeTable.Name}";
                using var reader = command.ExecuteReader();
                while(reader.Read())
                {
                    var eventType = new TypeId(reader.GetGuid(1));
                    var eventTypeId = reader.GetInt32(0);
                    Type foundEventType = null;

                    try
                    {
                        foundEventType = _typeMapper.GetType(eventType);
                    }
                    catch(CouldNotFindTypeForTypeIdException)
                    {
                        this.Log().Warning($"The type of event: Id: {eventTypeId}, Name: {eventType} that exists in the database could not be found in the loaded assemblies. No mapping will be created for this class. If an event of this type is read from the store an exception will be thrown");
                    }

                    if(foundEventType != null)
                    {
                        types.Add(new IdTypeMapping(id: eventTypeId, type: foundEventType));
                    } else
                    {
                        types.Add(new BrokenIdTypeMapping(id: eventTypeId, typeId: eventType));
                    }
                }
            }

            return types;
        });

        Dictionary<int, IIdTypeMapping> _idToTypeMap;
        Dictionary<Type, int> _typeToIdMap;
        readonly object _lockObject = new object();

        interface IIdTypeMapping
        {
            int Id { get; }
            Type Type { get; }
        }

        class BrokenIdTypeMapping : IIdTypeMapping
        {
            readonly TypeId _typeId;
            public BrokenIdTypeMapping(int id, TypeId typeId)
            {
                _typeId = typeId;
                Id = id;
            }
            public int Id { get; }
            public Type Type => throw new TryingToReadEventOfTypeThatNoMappingCouldBeFoundForException(_typeId, Id);
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
        public TryingToReadEventOfTypeThatNoMappingCouldBeFoundForException(TypeId typeId, int id) : base($"Event type Id: {id}, Name: {typeId} could not be mapped to a type.") {}
    }
}
