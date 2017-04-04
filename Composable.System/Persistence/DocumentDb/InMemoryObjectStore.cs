using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Composable.DDD;
using Composable.System.Collections.Collections;
using Composable.System.Linq;

namespace Composable.Persistence.DocumentDb
{
    class InMemoryObjectStore : IEnumerable<KeyValuePair<string, object>>
    {
        readonly Dictionary<string, List<Object>> _db = new Dictionary<string, List<object>>(StringComparer.InvariantCultureIgnoreCase);
        protected readonly object LockObject = new object();

        internal bool Contains(Type type, object id)
        {
            lock(LockObject)
            {
                object value;
                return TryGet(type, id, out value);
            }
        }

        internal bool TryGet<T>(object id, out T value)
        {
            lock(LockObject)
            {
                object found;
                if(TryGet(typeof(T), id, out found))
                {
                    value = (T)found;
                    return true;
                }
                value = default(T);
                return false;
            }
        }

        protected bool TryGet(Type typeOfValue, object id, out object value)
        {
            var idstring = GetIdString(id);
            value = null;

            List<Object> matchesId;
            if(!_db.TryGetValue(idstring, out matchesId))
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

        protected static string GetIdString(object id) => id.ToString().ToLower().TrimEnd(' ');

        public virtual void Add<T>(object id, T value)
        {
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

        public void Remove<T>(object id)
        {
            lock(LockObject)
            {
                Remove(id, typeof(T));
            }
        }

        public int RemoveAll<T>()
        {
            var toRemove = _db.Where(mapping => mapping.Value.Any( value => value.GetType() == typeof(T))).ToList();
            toRemove.ForEach(
                removeMe => _db.GetOrAddDefault(removeMe.Key).RemoveWhere(value => typeof(T) == value.GetType()));
            return toRemove.Count;
        }

        public void Remove(object id, Type documentType)
        {
            lock(LockObject)
            {
                var idstring = GetIdString(id);
                var removed = _db.GetOrAddDefault(idstring).RemoveWhere(documentType.IsInstanceOfType);
                if(removed < 1)
                {
                    throw new NoSuchDocumentException(id, documentType);
                }
                if (removed > 1)
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

        public void Update(IEnumerable<KeyValuePair<string, object>> values, Dictionary<Type, Dictionary<string, string>> persistentValues)
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
                object existing;
                if(!TryGet(value.GetType(), key, out existing))
                {
                    throw new NoSuchDocumentException(key, value.GetType());
                }
                if(!ReferenceEquals(existing, value))
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