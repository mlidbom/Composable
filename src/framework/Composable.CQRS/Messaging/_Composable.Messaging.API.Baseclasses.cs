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

    public class SingletonQuery<TSingleton> : Message, IQuery<TSingleton>
    { }

    ///<summary>Represent an entity within the domain of the current API that is uniquely identifiable through its type and Id.</summary>
    public interface IEntityResource<TResource>
    {
        Guid Id { get; }
    }

    class EntityQuery<TEntity> : Message, IEntityQuery<TEntity>
    {
        public EntityQuery(Guid id) => Id = id;
        public Guid Id { get; }
    }

    public abstract class Message : IMessage
    {
        protected Message() : this(Guid.NewGuid()) { }
        protected Message(Guid id) => MessageId = id;

        public Guid MessageId { get; private set; }//Do not remove setter. Required for serialization
    }

    public abstract class EntityResource<TResource> : Message, IEntityResource<TResource> where TResource : EntityResource<TResource>
    {
        protected EntityResource(Guid id)
        {
            Id = id;
            Self = new EntityQuery<TResource>(id);
        }
        public IQuery<TResource> Self { get; }
        public Guid Id { get; }
    }
}
