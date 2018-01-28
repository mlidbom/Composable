using System;
using Composable.DDD;

namespace Composable.Messaging
{
    public abstract class QueryResult {}

    ///<summary>Represent an entity within the domain of the current API that is uniquely identifiable through its type and Id.</summary>
    public interface IEntityResource : IHasPersistentIdentity<Guid>
    {
    }

    public abstract class EntityResource<TResource> : IEntityResource where TResource : EntityResource<TResource>
    {
        protected EntityResource() {}
        protected EntityResource(Guid id) => Id = id;
        public Guid Id { get; private set; }
    }
}
