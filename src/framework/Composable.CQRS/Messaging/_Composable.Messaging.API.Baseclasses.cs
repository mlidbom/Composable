using System;
using Composable.Messaging.Commands;

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

    public class AggregateLink<TEntity> : Message, IQuery<TEntity>
    {
        public AggregateLink() {}
        public AggregateLink(Guid id) => Id = id;
        public Guid Id { get; set; }
        public AggregateLink<TEntity> WithId(Guid id) => new AggregateLink<TEntity> {Id = id};
    }

    public class ReadonlyCopyOfEntityByIdQuery<TEntity> : Message, IQuery<TEntity>
    {
        public ReadonlyCopyOfEntityByIdQuery() {}
        public ReadonlyCopyOfEntityByIdQuery(Guid id) => Id = id;
        public Guid Id { get; set; }
        public AggregateLink<TEntity> WithId(Guid id) => new AggregateLink<TEntity> {Id = id};
    }

    public class ReadonlyCopyOfEntityVersionByIdQuery<TEntity> : Message, IQuery<TEntity>
    {
        public ReadonlyCopyOfEntityVersionByIdQuery() {}
        public ReadonlyCopyOfEntityVersionByIdQuery(Guid id, int version)
        {
            Id = id;
            Version = version;
        }

        public Guid Id { get; set; }
        public int Version { get; set;}
        public AggregateLink<TEntity> WithId(Guid id) => new AggregateLink<TEntity> {Id = id};
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

    public class PersistEntityCommand<TEntity> : ExactlyOnceCommand
    {
        public PersistEntityCommand(TEntity entity) => Entity = entity;
        public TEntity Entity { get; }
    }
}
