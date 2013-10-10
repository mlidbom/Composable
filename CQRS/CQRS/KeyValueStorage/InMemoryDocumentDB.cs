using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Castle.Components.DictionaryAdapter.Xml;
using Composable.DDD;
using Composable.NewtonSoft;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using Newtonsoft.Json;

namespace Composable.KeyValueStorage
{
    public class InMemoryDocumentDb : InMemoryObjectStore, IDocumentDb
    {
        private readonly ISet<IObserver<IDocumentUpdated>> _observers = new HashSet<IObserver<IDocumentUpdated>>();

        public InMemoryDocumentDb()
        {
            lock(_lockObject)
            {
                DocumentUpdated = Observable.Create<IDocumentUpdated>(
                    obs =>
                    {
                        _observers.Add(obs);
                        return Disposable.Create(() => _observers.Remove(obs));
                    });
            }
        }

        public IObservable<IDocumentUpdated> DocumentUpdated { get; private set; }

        private void NotifySubscribersDocumentUpdated(string key, object document)
        {
            _observers.ToArray().ForEach(observer => observer.OnNext(new DocumentUpdated(key, document)));
        }

        private readonly Dictionary<Type, Dictionary<string, string>> _persistentValues = new Dictionary<Type, Dictionary<string, string>>();

        public bool TryGet<T>(object id, out T value, Dictionary<Type, Dictionary<string, string>> persistentValues)
        {
            return TryGet<T>(id, out value);
        }

        public void Add<T>(object id, T value, Dictionary<Type, Dictionary<string, string>> persistentValues)
        {
            lock(_lockObject)
            {
                var idString = GetIdString(id);
                var stringValue = JsonConvert.SerializeObject(value, JsonSettings.JsonSerializerSettings);
                SetPersistedValue(value, idString, stringValue);
                base.Add(id, value);
                NotifySubscribersDocumentUpdated(idString, value);
            }
        }

        public IEnumerable<T> GetAll<T>(IEnumerable<Guid> ids) where T : IHasPersistentIdentity<Guid>
        {
            return GetAll<T>().Where(document => ids.Contains(document.Id));
        }

        private void SetPersistedValue<T>(T value, string idString, string stringValue)
        {
            _persistentValues.GetOrAddDefault(value.GetType())[idString] = stringValue;
        }

        override public void Update(object key, object value)
        {
            lock(_lockObject)
            {
                string oldValue;
                string idString = GetIdString(key);
                var stringValue = JsonConvert.SerializeObject(value, JsonSettings.JsonSerializerSettings);
                var needsUpdate = !_persistentValues
                    .GetOrAdd(value.GetType(), () => new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase))
                    .TryGetValue(idString, out oldValue) || stringValue != oldValue;

                if(!needsUpdate)
                {
                    object existingValue;
                    base.TryGet(value.GetType(), key, out existingValue);
                    needsUpdate = !(ReferenceEquals(existingValue, value));
                }

                if(needsUpdate)
                {
                    base.Update(key, value);
                    SetPersistedValue(value, idString, stringValue);
                    NotifySubscribersDocumentUpdated(idString, value);
                }
            }
        }
    }
}
