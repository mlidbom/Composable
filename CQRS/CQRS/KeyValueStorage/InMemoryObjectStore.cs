using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Composable.DDD;
using Composable.System.Collections.Collections;
using Composable.System.Linq;

namespace Composable.KeyValueStorage
{
    public class InMemoryObjectStore : IEnumerable<KeyValuePair<string, object>>, IObjectStore
    {
        private Dictionary<string, List<Object>> _db = new Dictionary<string, List<object>>(StringComparer.InvariantCultureIgnoreCase);
        public bool Contains<T>(object id)
        {
            T value;
            return TryGet(id, out value);
        }

        public bool Contains(Type type, object id)
        {
            object value;
            return TryGet(type, id, out value);
        }

        public bool TryGet<T>(object id, out T value)
        {
            object found;
            if(TryGet(typeof(T), id, out found))
            {
                value = (T) found;
                return true;
            }
            value = default(T);
            return false;
        }

        private bool TryGet(Type typeOfValue, object id, out object value)
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

        private static string GetIdString(object id)
        {
            return id.ToString().ToLower().TrimEnd(' ');
        }

        public void Add<T>(object id, T value)
        {
            var idString = GetIdString(id);
            if(Contains(value.GetType(), idString))
            {
                throw new AttemptToSaveAlreadyPersistedValueException(id, value);
            }
            _db.GetOrAddDefault(idString).Add(value);
        }

        public bool Remove<T>(object id)
        {
            return Remove(id, typeof(T));
        }

        public bool Remove(object id, Type documentType)
        {
            var idstring = GetIdString(id);
            var removed = _db.GetOrAddDefault(idstring).RemoveWhere(value => documentType.IsAssignableFrom(value.GetType()));
            if (removed > 1)
            {
                throw new Exception("FUBAR");
            }
            return removed == 1;
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _db.SelectMany(m => m.Value.Select(inner => new KeyValuePair<string,object>(m.Key, inner))).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Update(IEnumerable<KeyValuePair<string, object>> values)
        {
            values.ForEach( pair => Update(pair.Key, pair.Value));
        }

        public void Update(object key, object value)
        {
            object existing;
            if(!TryGet(value.GetType(), key, out existing))
            {
                throw new NoSuchDocumentException(key, value.GetType());
            }
            if(!ReferenceEquals(value, existing))
            {
                throw new Exception("FUBAR");
            }
        }

        public IEnumerable<KeyValuePair<Guid, T>> GetAll<T>() where T : IHasPersistentIdentity<Guid>
        {
            return this.
                Where(pair => typeof(T).IsAssignableFrom(pair.Value.GetType()))
                .Select(pair => new KeyValuePair<Guid, T>(Guid.Parse(pair.Key), (T) pair.Value))
                .ToList();
        }

        public void Dispose()
        {
            //Not really anything much to do here....
        }

        public void Clear()
        {
            _db = new Dictionary<string, List<object>>();
        }
    }
}