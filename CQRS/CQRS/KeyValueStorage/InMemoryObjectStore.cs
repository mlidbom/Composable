using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Composable.System.Collections.Collections;
using Composable.System.Linq;

namespace Composable.KeyValueStorage
{
    public class InMemoryObjectStore : IEnumerable<KeyValuePair<Guid, object>>, IObjectStore
    {
        private List<KeyValuePair<Guid, object>> _db = new List<KeyValuePair<Guid, object>>();
        public bool Contains<T>(Guid id)
        {
            T value;
            return TryGet(id, out value);
        }

        public bool Contains(Type type, Guid id)
        {
            object value;
            return TryGet(type, id, out value);
        }

        public bool TryGet<T>(Guid id, out T value)
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

        private bool TryGet(Type typeOfValue, Guid id, out object value)
        {
            var found = _db
                .Where(pair => pair.Key == id)
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

        public void Add<T>(Guid id, T value)
        {
            if(Contains(value.GetType(), id))
            {
                throw new AttemptToSaveAlreadyPersistedValueException(id, value);
            }
            _db.Add(new KeyValuePair<Guid, object>(id, value));
        }

        public bool Remove<T>(Guid id)
        {
            var removed = _db.RemoveWhere(pair => pair.Key == id && pair.Value is T);
            if(removed > 1)
            {
                throw new Exception("FUBAR");
            }
            return removed == 1;
        }

        public IEnumerator<KeyValuePair<Guid, object>> GetEnumerator()
        {
            return _db.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Update(IEnumerable<KeyValuePair<Guid, object>> values)
        {
            values.ForEach( pair => Update(pair.Key, pair.Value));
        }

        public void Update(Guid key, object value)
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

        public IEnumerable<KeyValuePair<Guid, T>> GetAll<T>()
        {
            return _db
                .Where(pair => typeof(T).IsAssignableFrom(pair.Value.GetType()))
                .Select(pair => new KeyValuePair<Guid, T>(pair.Key, (T) pair.Value))
                .ToList();
        }

        public void Dispose()
        {
            //Not really anything much to do here....
        }
    }
}