using System;
using System.Collections.Generic;
using Composable.DDD;

namespace Composable.KeyValueStorage
{
    public interface IDocumentDb : IDocumentUpdatedNotifier
    {
        bool TryGet<T>(object id, out T value, Dictionary<Type,Dictionary<string,string>> persistentValues);
        void Add<T>(object id, T value, Dictionary<Type, Dictionary<string, string>> persistentValues);
        void Update(IEnumerable<KeyValuePair<string, object>> values, Dictionary<Type, Dictionary<string, string>> persistentValues);

        bool Remove<T>(object id);
        bool Remove(object id, Type documentType);
        IEnumerable<T> GetAll<T>() where T : IHasPersistentIdentity<Guid>;
        IEnumerable<T> GetAll<T>(IEnumerable<Guid> ids) where T : IHasPersistentIdentity<Guid>;
        IEnumerable<Guid> GetAllIds<T>() where T : IHasPersistentIdentity<Guid>;
    }
}