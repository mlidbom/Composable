using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Composable.Contracts;
using Composable.DDD;
using Composable.System.Collections.Collections;
using Composable.System.Linq;

namespace Composable.Persistence.DocumentDb
{
    class MemoryObjectStore : IEnumerable<KeyValuePair<string, object>>
    {
        readonly Dictionary<string, List<Object>> _db = new Dictionary<string, List<object>>(StringComparer.InvariantCultureIgnoreCase);
        protected readonly object LockObject = new object();

        internal bool Contains(Type type, object id)
        {
            lock(LockObject)
            {
                return TryGet(type, id, out _);
            }
        }

        internal bool TryGet<T>(object id, [NotNullWhen(true)]out T value)
        {
            lock(LockObject)
            {
                if (TryGet(typeof(T), id, out var found))
                {
                    value = (T)found;
                    return true;
                }
                value = default!;
                return false;
            }
        }

        protected bool TryGet(Type typeOfValue, object id, [NotNullWhen(true)]out object? value)
        {
            var idstring = GetIdString(id);
            value = null;

            if (!_db.TryGetValue(idstring, out var matchesId))
            {
                return false;
            }

            var found = matchesId.Where(typeOfValue.IsInstanceOfType).ToList();
            if(found.Any())
            {
                value = found.Single();
                return true;
            }
            return false;
        }

        protected static string GetIdString(object id) => id.ToString().ToUpperInvariant().TrimEnd(' ');

        public virtual void Add<T>(object id, T value)
        {
            Assert.Argument.NotNull(value);
            lock(LockObject)
            {
                var idString = GetIdString(id);
                if(Contains(value.GetType(), idString))
                {
                    throw new AttemptToSaveAlreadyPersistedValueException(id, value);
                }
                _db.GetOrAddDefault(idString).Add(value);
            }
        }

        public void Remove(object id, Type documentType)
        {
            lock(LockObject)
            {
                var idstring = GetIdString(id);
                var removed = _db.GetOrAddDefault(idstring).RemoveWhere(documentType.IsInstanceOfType);
                if(removed.None())
                {
                    throw new NoSuchDocumentException(id, documentType);
                }
                if (removed.Count > 1)
                {
                    throw new Exception("It really should be impossible to hit multiple documents with one Id, but apparently you just did it!");
                }
            }
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            lock(LockObject)
            {
                return _db.SelectMany(m => m.Value.Select(inner => new KeyValuePair<string, object>(m.Key, inner)))
                    .ToList()//ToList is to make it thread safe...
                    .GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Update(IEnumerable<KeyValuePair<string, object>> values, Dictionary<Type, Dictionary<string, string>> _)
        {
            lock(LockObject)
            {
                values.ForEach(pair => Update(pair.Key, pair.Value));
            }
        }

        protected virtual void Update(object key, object value)
        {
            lock(LockObject)
            {
                if (!TryGet(value.GetType(), key, out var existing))
                {
                    throw new NoSuchDocumentException(key, value.GetType());
                }
                if (!ReferenceEquals(existing, value))
                {
                    Remove(key, value.GetType());
                    Add(key, value);
                }
            }
        }

        public IEnumerable<T> GetAll<T>() where T : IHasPersistentIdentity<Guid>
        {
            lock(LockObject)
            {
                return this.
                    Where(pair => pair.Value is T)
                    .Select(pair => (T)pair.Value)
                    .ToList();
            }
        }
    }
}