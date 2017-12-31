using System;
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedMember.Global

namespace Composable.Messaging
{
    public abstract class QueryResult : Message
    {
    }

    public abstract class Event : Message, IEvent
    {
    }

    public abstract class Query<TResult> : Message, IQuery<TResult>
    {
    }

    ///<summary>Represent an entity within the domain of the current API that is uniquely identifiable through its type and Id.</summary>
    public interface IEntityResource<TResource>
    {
        Guid Id { get; }
    }

    public abstract class EntityByIdQuery<TEntity, TInheritor> : Message, IEntityQuery<TEntity> where TInheritor : EntityByIdQuery<TEntity, TInheritor>, new()
    {
        public EntityByIdQuery() {}
        public EntityByIdQuery(Guid id) => Id = id;
        public Guid Id { get; set; }
        public TInheritor WithId(Guid id) => new TInheritor{ Id = id};
    }

    public abstract class Message : IMessage
    {
        protected Message() : this(Guid.NewGuid()) { }
        protected Message(Guid id) => MessageId = id;

        public Guid MessageId { get; private set; }//Do not remove setter. Required for serialization
    }

    public abstract class EntityResource<TResource> : Message, IEntityResource<TResource> where TResource : EntityResource<TResource>
    {
        protected EntityResource() {}
        protected EntityResource(Guid id)
        {
            Id = id;
        }
        public Guid Id { get; private set; }
    }
}
