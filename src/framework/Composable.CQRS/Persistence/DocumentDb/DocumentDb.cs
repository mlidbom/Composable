using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Composable.Contracts;
using Composable.DDD;
using Composable.GenericAbstractions.Time;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System.Collections.Collections;

namespace Composable.Persistence.DocumentDb
{
    class DocumentDb : IDocumentDb
    {
        readonly IUtcTimeTimeSource _timeSource;
        readonly IDocumentDbSerializer _serializer;

        readonly ITypeMapper _typeMapper;
        readonly IDocumentDbPersistenceLayer _persistenceLayer;

        internal DocumentDb(IUtcTimeTimeSource timeSource, IDocumentDbSerializer serializer, ITypeMapper typeMapper, IDocumentDbPersistenceLayer persistenceLayer)
        {
            _persistenceLayer = persistenceLayer;
            _timeSource = timeSource;
            _serializer = serializer;
            _typeMapper = typeMapper;
        }

        bool IDocumentDb.TryGet<TValue>(object id, [NotNullWhen(true)] [MaybeNull] out TValue value, Dictionary<Type, Dictionary<string, string>> persistentValues)
        {
            value = default;
            var idString = GetIdString(id);

            if(!_persistenceLayer.TryGet(idString, GetAcceptableTypeGuids(typeof(TValue)), DocumentDbSession.UseUpdateLock, out var readRow)) return false;

            var found = _serializer.Deserialize(GetTypeFromId(new TypeId(readRow.TypeGuid)), readRow.SerializedValue);

            //Things such as TimeZone etc can cause roundtripping serialization to result in different values from the original so don't cache the read string. Cache the result of serializing it again.
            //performance: Try to find a way to remove the need to do this so that we can get rid of the overhead of an extra serialization.
            persistentValues.GetOrAddDefault(found.GetType())[idString] = _serializer.Serialize(found);

            value = (TValue)found;
            return true;
        }

        public void Add<T>(object id, T value, Dictionary<Type, Dictionary<string, string>> persistentValues)
        {
            Assert.Argument.NotNull(value);

            var idString = GetIdString(id);
            var serializedDocument = _serializer.Serialize(value);

            _persistenceLayer.Add(idString, _typeMapper.GetId(value.GetType()).GuidValue, _timeSource.UtcNow, serializedDocument);

            persistentValues.GetOrAddDefault(value.GetType())[idString] = serializedDocument;
        }


        static string GetIdString(object id) => id.ToString().ToLower().TrimEnd(' ');

        public void Remove(object id, Type documentType)
        {
            var rowsAffected = _persistenceLayer.Remove(GetIdString(id), GetAcceptableTypeGuids(documentType));

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
            values = values.ToList();

            var toUpdate = new List<IDocumentDbPersistenceLayer.WriteRow>();
            var now = _timeSource.UtcNow;
            foreach(var item in values)
            {
                var serializedDocument = _serializer.Serialize(item.Value);
                var needsUpdate = !persistentValues.GetOrAddDefault(item.Value.GetType()).TryGetValue(item.Key, out var oldValue) || serializedDocument != oldValue;
                if(needsUpdate)
                {
                    persistentValues.GetOrAddDefault(item.Value.GetType())[item.Key] = serializedDocument;
                    toUpdate.Add(new IDocumentDbPersistenceLayer.WriteRow(item.Key, serializedDocument, now, _typeMapper.GetId(item.Value.GetType()).GuidValue));
                }
            }

            _persistenceLayer.Update(toUpdate);
        }

        IEnumerable<T> IDocumentDb.GetAll<T>()
        {
            var acceptableTypeGuids = GetAcceptableTypeGuids<T>();

            var storedList = _persistenceLayer.GetAll(acceptableTypeGuids);

            return storedList.Select(stored => (T)_serializer.Deserialize(GetTypeFromId(new TypeId(stored.TypeGuid)), stored.SerializedValue));
        }

        public IEnumerable<T> GetAll<T>(IEnumerable<Guid> ids) where T : IHasPersistentIdentity<Guid>
        {
            Type type = typeof(T);

            var storedList = _persistenceLayer.GetAll(ids, GetAcceptableTypeGuids(type));

            return storedList.Select(stored => (T)_serializer.Deserialize(GetTypeFromId(new TypeId(stored.TypeGuid)), stored.SerializedValue));
        }

        public IEnumerable<Guid> GetAllIds<T>() where T : IHasPersistentIdentity<Guid> => _persistenceLayer.GetAllIds(GetAcceptableTypeGuids(typeof(T)));

        IReadOnlyList<Guid> GetAcceptableTypeGuids<T>() => GetAcceptableTypeGuids(typeof(T));
        IReadOnlyList<Guid> GetAcceptableTypeGuids(Type type) => _typeMapper.GetIdForTypesAssignableTo(type).Select(typeId => typeId.GuidValue).ToList();
        Type GetTypeFromId(TypeId id) => _typeMapper.GetType(id);
    }
}
