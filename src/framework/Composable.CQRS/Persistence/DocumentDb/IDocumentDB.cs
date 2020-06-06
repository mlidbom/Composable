using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Composable.DDD;

namespace Composable.Persistence.DocumentDb
{
    interface IDocumentDb
    {
        bool TryGet<T>(object id, [NotNullWhen(true)][MaybeNull]out T value, Dictionary<Type,Dictionary<string,string>> persistentValues);
        void Add<T>(object id, T value, Dictionary<Type, Dictionary<string, string>> persistentValues);
        void Update(IEnumerable<KeyValuePair<string, object>> values, Dictionary<Type, Dictionary<string, string>> persistentValues);

        // ReSharper disable once UnusedMember.Global
        void Remove<T>(object id);
        int RemoveAll<T>();
        void Remove(object id, Type documentType);
        IEnumerable<T> GetAll<T>() where T : IHasPersistentIdentity<Guid>;
        IEnumerable<T> GetAll<T>(IEnumerable<Guid> ids) where T : IHasPersistentIdentity<Guid>;
        IEnumerable<Guid> GetAllIds<T>() where T : IHasPersistentIdentity<Guid>;
    }
}