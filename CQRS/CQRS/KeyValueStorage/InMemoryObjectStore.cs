using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Composable.DDD;
using Composable.System.Collections.Collections;
using Composable.System.Linq;

namespace Composable.KeyValueStorage
{
    public class InMemoryObjectStore : IEnumerable<KeyValuePair<object, object>>, IObjectStore
    {
        private List<KeyValuePair<object, object>> _db = new List<KeyValuePair<object, object>>();
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
            var idstring = id.ToString();
            var found = _db
                .Where(pair => pair.Key.ToString() == idstring)
                .Select(pair => pair.Value)
                .Where(obj => typeOfValue.IsAssignableFrom(obj.GetType()))
                .ToList();
            if(found.Any())
            {
                value = found.Single();
                return true;
            }
            value = null;
            return false;
        }

        public void Add<T>(object id, T value)
        {
            if(Contains(value.GetType(), id.ToString()))
            {
                throw new AttemptToSaveAlreadyPersistedValueException(id, value);
            }
            _db.Add(new KeyValuePair<object, object>(id, value));
        }

        public bool Remove<T>(object id)
        {
            var idstring = id.ToString();
            var removed = _db.RemoveWhere(pair => pair.Key.ToString() == idstring && pair.Value is T);
            if(removed > 1)
            {
                throw new Exception("FUBAR");
            }
            return removed == 1;
        }

        public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
        {
            return _db.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Update(IEnumerable<KeyValuePair<object, object>> values)
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
            return _db
                .Where(pair => typeof(T).IsAssignableFrom(pair.Value.GetType()))
                .Select(pair => new KeyValuePair<Guid, T>((Guid)pair.Key, (T) pair.Value))
                .ToList();
        }

        public void Dispose()
        {
            //Not really anything much to do here....
        }
    }
}