using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Composable.DDD;
using Composable.System.Collections.Collections;
using Composable.System.Linq;

namespace Composable.KeyValueStorage
{
    public class InMemoryObjectStore : IEnumerable<KeyValuePair<string, object>>
    {
        Dictionary<string, List<Object>> _db = new Dictionary<string, List<object>>(StringComparer.InvariantCultureIgnoreCase);
        protected object _lockObject = new object();

        public bool Contains(Type type, object id)
        {
            lock(_lockObject)
            {
                object value;
                return TryGet(type, id, out value);
            }
        }

        public bool TryGet<T>(object id, out T value)
        {
            lock(_lockObject)
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

            List<Object> matchesId = null;
            if(!_db.TryGetValue(idstring, out matchesId))
            {
                return false;
            }

            var found = matchesId.Where(obj => typeOfValue.IsAssignableFrom(obj.GetType())).ToList();
            if(found.Any())
            {
                value = found.Single();
                return true;
            }
            return false;
        }

        protected static string GetIdString(object id)
        {
            return id.ToString().ToLower().TrimEnd(' ');
        }

        public virtual void Add<T>(object id, T value)
        {
            lock(_lockObject)
            {
                var idString = GetIdString(id);
                if(Contains(value.GetType(), idString))
                {
                    throw new AttemptToSaveAlreadyPersistedValueException(id, value);
                }
                _db.GetOrAddDefault(idString).Add(value);
            }
        }

        public bool Remove<T>(object id)
        {
            lock(_lockObject)
            {
                return Remove(id, typeof(T));
            }
        }

        public int RemoveAll<T>()
        {
            var toRemove = _db.Where(mapping => mapping.Value.Any( value => value.GetType() == typeof(T))).ToList();
            toRemove.ForEach(
                removeMe =>
                {
                    _db.GetOrAddDefault(removeMe.Key).RemoveWhere(value => typeof(T) == value.GetType());
                });
            return toRemove.Count;
        }

        public bool Remove(object id, Type documentType)
        {
            lock(_lockObject)
            {
                var idstring = GetIdString(id);
                var removed = _db.GetOrAddDefault(idstring).RemoveWhere(value => documentType.IsAssignableFrom(value.GetType()));
                if(removed > 1)
                {
                    throw new Exception("FUBAR");
                }
                return removed == 1;
            }
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            lock(_lockObject)
            {
                return _db.SelectMany(m => m.Value.Select(inner => new KeyValuePair<string, object>(m.Key, inner)))
                    .ToList()//ToList is to make it thread safe...
                    .GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Update(IEnumerable<KeyValuePair<string, object>> values, Dictionary<Type, Dictionary<string, string>> persistentValues)
        {
            lock(_lockObject)
            {
                values.ForEach(pair => Update(pair.Key, pair.Value));
            }
        }

        public virtual void Update(object key, object value)
        {
            lock(_lockObject)
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
            lock(_lockObject)
            {
                return this.
                    Where(pair => typeof(T).IsAssignableFrom(pair.Value.GetType()))
                    .Select(pair => (T)pair.Value)
                    .ToList();
            }
        }
    }
}