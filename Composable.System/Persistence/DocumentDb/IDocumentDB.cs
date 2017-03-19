using System;
using System.Collections.Generic;
using Composable.DDD;

namespace Composable.Persistence.DocumentDb
{
    public interface IDocumentDb
    {
        bool TryGet<T>(object id, out T value, Dictionary<Type,Dictionary<string,string>> persistentValues);
        void Add<T>(object id, T value, Dictionary<Type, Dictionary<string, string>> persistentValues);
        void Update(IEnumerable<KeyValuePair<string, object>> values, Dictionary<Type, Dictionary<string, string>> persistentValues);

        void Remove<T>(object id);
        int RemoveAll<T>();
        void Remove(object id, Type documentType);
        IEnumerable<T> GetAll<T>() where T : IHasPersistentIdentity<Guid>;
        IEnumerable<T> GetAll<T>(IEnumerable<Guid> ids) where T : IHasPersistentIdentity<Guid>;
        IEnumerable<Guid> GetAllIds<T>() where T : IHasPersistentIdentity<Guid>;
    }
}