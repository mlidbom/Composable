using System;
using Composable.DDD;

namespace Composable.Messaging
{
    public abstract class EntityResource<TResource> : IHasPersistentIdentity<Guid> where TResource : EntityResource<TResource>
    {
        protected EntityResource() {}
        protected EntityResource(Guid id) => Id = id;
        public Guid Id { get; private set; }
    }
}
