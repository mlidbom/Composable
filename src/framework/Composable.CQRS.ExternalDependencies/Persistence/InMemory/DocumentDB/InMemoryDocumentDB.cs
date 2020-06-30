using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Composable.Contracts;
using Composable.DDD;
using Composable.Persistence.DocumentDb;
using Composable.Serialization;
using Composable.System.Collections.Collections;

namespace Composable.Persistence.InMemory.DocumentDB
{
    [Obsolete("About to be removed", error:true)]
    class InMemoryDocumentDb : MemoryObjectStore, IDocumentDb
    {
        readonly IDocumentDbSerializer _serializer;
        public InMemoryDocumentDb(IDocumentDbSerializer serializer) => _serializer = serializer;
        readonly Dictionary<Type, Dictionary<string, string>> _persistentValues = new Dictionary<Type, Dictionary<string, string>>();

        public bool TryGet<T>(object id, [NotNullWhen(true)]out T value, Dictionary<Type, Dictionary<string, string>> persistentValues) => TryGet(id, out value);

        public void Add<T>(object id, T value, Dictionary<Type, Dictionary<string, string>> persistentValues)
        {
            Assert.Argument.NotNull(value);
            lock(LockObject)
            {
                var idString = GetIdString(id);
                var stringValue = _serializer.Serialize(value);
                SetPersistedValue(value, idString, stringValue);
                base.Add(id, value);
            }
        }

        public IEnumerable<T> GetAll<T>(IEnumerable<Guid> ids) where T : IHasPersistentIdentity<Guid>
        {
            return GetAll<T>().Where(document => ids.Contains(document.Id));
        }

        public IEnumerable<Guid> GetAllIds<T>() where T : IHasPersistentIdentity<Guid>
        {
            return GetAll<T>().Select(document => document.Id);
        }

        void SetPersistedValue<T>(T value, string idString, string stringValue)
        {
            Assert.Argument.NotNull(value);
            _persistentValues.GetOrAddDefault(value.GetType())[idString] = stringValue;
        }

        protected override void Update(object key, object value)
        {
            lock(LockObject)
            {
                var idString = GetIdString(key);
                var stringValue = _serializer.Serialize(value);
                var needsUpdate = !_persistentValues
                    .GetOrAdd(value.GetType(), () => new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase))
                    .TryGetValue(idString, out var oldValue) || stringValue != oldValue;

                if(!needsUpdate)
                {
                    base.TryGet(value.GetType(), key, out var existingValue);
                    needsUpdate = !(ReferenceEquals(existingValue, value));
                }

                if(needsUpdate)
                {
                    base.Update(key, value);
                    SetPersistedValue(value, idString, stringValue);
                }
            }
        }
    }
}
