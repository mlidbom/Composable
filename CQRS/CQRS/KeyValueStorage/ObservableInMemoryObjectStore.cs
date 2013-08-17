using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Composable.NewtonSoft;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using Newtonsoft.Json;

namespace Composable.KeyValueStorage
{
    public class ObservableInMemoryObjectStore : InMemoryObjectStore, IObservableObjectStore
    {
        private readonly ISet<IObserver<IDocumentUpdated>> _observers = new HashSet<IObserver<IDocumentUpdated>>();

        public ObservableInMemoryObjectStore()
        {
            DocumentUpdated = Observable.Create<IDocumentUpdated>(
                obs =>
                {
                    _observers.Add(obs);
                    return Disposable.Create(() => _observers.Remove(obs));
                });
        }

        public IObservable<IDocumentUpdated> DocumentUpdated { get; private set; }

        private void NotifySubscribersDocumentUpdated(string key, object document)
        {
            _observers.ForEach(observer => observer.OnNext(new DocumentUpdated(key, document)));
        }

        private readonly Dictionary<Type, Dictionary<string, string>> _persistentValues = new Dictionary<Type, Dictionary<string, string>>();

        override public void Add<T>(object id, T value)
        {
            var idString = GetIdString(id);
            var stringValue = JsonConvert.SerializeObject(value, JsonSettings.JsonSerializerSettings);
            _persistentValues.GetOrAddDefault(value.GetType())[idString] = stringValue;
            base.Add(id, value);
            NotifySubscribersDocumentUpdated(idString, value);
        }

        override public void Update(object key, object value)
        {
            string oldValue;
            string idString = GetIdString(key);
            var stringValue = JsonConvert.SerializeObject(value, JsonSettings.JsonSerializerSettings);
            var needsUpdate = !_persistentValues
                .GetOrAdd(value.GetType(), () => new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase))
                .TryGetValue(idString, out oldValue) || stringValue != oldValue;
            if(needsUpdate)
            {
                base.Update(key, value);
                NotifySubscribersDocumentUpdated(idString, value);
            }
        }
    }
}
