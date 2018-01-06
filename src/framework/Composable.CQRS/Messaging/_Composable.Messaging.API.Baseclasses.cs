using System;

// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedMember.Global

namespace Composable.Messaging
{
    public abstract class QueryResult : Message {}

    public abstract class Query<TResult> : Message, IQuery<TResult> {}

    ///<summary>Represent an entity within the domain of the current API that is uniquely identifiable through its type and Id.</summary>
    public interface IEntityResource<TResource>
    {
        Guid Id { get; }
    }

    [TypeId("444153B1-7B35-4F17-9FF3-85040CEEBAAB")]
    public class EntityByIdQuery<TEntity> : Message, IEntityQuery<TEntity>
    {
        public EntityByIdQuery() {}
        public EntityByIdQuery(Guid id) => Id = id;
        public Guid Id { get; set; }
        public EntityByIdQuery<TEntity> WithId(Guid id) => new EntityByIdQuery<TEntity> {Id = id};
    }

    public abstract class Message : IMessage
    {
        protected Message() : this(Guid.NewGuid()) {}
        protected Message(Guid id) => MessageId = id;

        public Guid MessageId { get; private set; } //Do not remove setter. Required for serialization
    }

    public abstract class EntityResource<TResource> : Message, IEntityResource<TResource> where TResource : EntityResource<TResource>
    {
        protected EntityResource() {}
        protected EntityResource(Guid id) => Id = id;
        public Guid Id { get; private set; }
    }
}
