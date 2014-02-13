using System;
using System.Collections.Generic;
using Composable.System.Linq;
using Composable.DDD;
using Composable.System.Collections.Collections;

namespace Composable.CQRS
{
    public static class HashedSetExtensions
    {
        public static void AddIfNotPresent<T>(this ISet<EntityReference<T>> set, Guid id)
            where T : IHasPersistentIdentity<Guid>, INamed
        {
            if (set.None(i => i.Id == id)) { set.Add(new EntityReference<T>(id)); }
        }

        public static void RemoveIfPresent<T>(this ISet<EntityReference<T>> set, Guid id)
            where T : IHasPersistentIdentity<Guid>, INamed
        {
            set.RemoveWhere(i => i.Id == id); 
        }

        public static void AddIfNotPresent<T>(this ISet<UnNamedEntityReference<T>> set, Guid id)
            where T : IHasPersistentIdentity<Guid>
        {
            if (set.None(i => i.Id == id)) { set.Add(new UnNamedEntityReference<T>(id)); }
        }

        public static void RemoveIfPresent<T>(this ISet<UnNamedEntityReference<T>> set, Guid id)
            where T : IHasPersistentIdentity<Guid>
        {
            set.RemoveWhere(i => i.Id == id); 
        }


        public static void AddIfNotPresent<T>(this ISet<EntityReference<T>> set, IEnumerable<Guid> ids)
            where T : IHasPersistentIdentity<Guid>, INamed
        {
            ids.ForEach(id => set.AddIfNotPresent(id));
        }

        public static void RemoveIfPresent<T>(this ISet<EntityReference<T>> set, IEnumerable<Guid> ids)
            where T : IHasPersistentIdentity<Guid>, INamed
        {
            ids.ForEach(id => set.RemoveIfPresent(id));
        }

        public static void AddIfNotPresent<T>(this ISet<UnNamedEntityReference<T>> set, IEnumerable<Guid> ids)
            where T : IHasPersistentIdentity<Guid>
        {
            ids.ForEach( id => set.AddIfNotPresent(id));
        }

        public static void RemoveIfPresent<T>(this ISet<UnNamedEntityReference<T>> set, IEnumerable<Guid> ids)
            where T : IHasPersistentIdentity<Guid>
        {
            ids.ForEach(id => set.RemoveIfPresent(id));
        }
    }
}