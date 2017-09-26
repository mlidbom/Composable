using System;
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedMember.Global

namespace Composable.Messaging
{
    ///<summary>An object that is used to transfer data from a sender to a receiver through a messaging infrastructure.</summary>
    public interface IMessage
    {
        Guid MessageId { get; }
    }

    public abstract class Message : IMessage
    {
        protected Message():this(Guid.NewGuid()) {}
        protected Message(Guid id) => MessageId = id;
        public Guid MessageId { get; }
    }

    ///<summary>An <see cref="IMessage"/> that instructs the receiver to perform an action.
    /// <para>Implementations should be named as an imperative sentence with an optional(but standardized "Command" suffix): RegisterUserAccount[Command]</para></summary>
    public interface ICommand : IMessage
    {}

    public interface ICommand<TResult> : ICommand where TResult : IMessage
    {
    }

    ///<summary>An <see cref="IMessage"/> which informs the receiver that something has happened.
    /// <para>Should be immutable since it is impossible to change something that has already happened.</para>
    /// </summary>
    public interface IEvent : IMessage {}

    public abstract class Event : Message, IEvent
    {
    }

    public interface IQuery : IMessage {}

    ///<summary>An <see cref="IMessage"/> that instructs the receiver to return some data.</summary>
    public interface IQuery<TResult> : IQuery where TResult : IQueryResult {}

    public abstract class Query<TResult> : Message, IQuery<TResult> where TResult : IQueryResult
    {
    }

    ///<summary>Represent an entity within the domain of the current API that is uniquely identifiable through a query.</summary>
    public interface IResource<TResource> : IQueryResult where TResource : IResource<TResource>
    {
        IQuery<TResource> Self { get; }
    }

    ///<summary>A response to an <see cref="IQuery{TResult}"/></summary>
    public interface IQueryResult : IMessage {}

    public abstract class QueryResult : Message, IQueryResult
    {
    }


    ///<summary>Any type that subscribes to an event should implement this interface. Regardless of wether the event was Published or Replayed.</summary>
    public interface IEventSubscriber<in TEvent> where TEvent : IEvent
    {
        void Handle(TEvent message);
    }

    public interface ICommandHandler<in TCommand> where TCommand : ICommand
    {
        void Handle(TCommand command);
    }

    public interface IQueryHandler<in TQuery, out TResult> where TQuery: IQuery<TResult>
                                                       where TResult : IQueryResult
    {
        TResult Handle(TQuery command);
    }


    public class SingletonQuery<TSingleton> : Message, IQuery<TSingleton> where TSingleton : IResource<TSingleton>
    {}

    interface IEntityQuery<TEntity> : IQuery<TEntity> where TEntity : IQueryResult
    {
        Guid Id { get; }
    }

    class EntityQuery<TEntity> : Message, IEntityQuery<TEntity> where TEntity : IResource<TEntity>
    {
        public EntityQuery(Guid id) => Id = id;
        public Guid Id { get; }
    }

    public interface IEntityResource<TResource> : IResource<TResource> where TResource : IEntityResource<TResource>
    {
        Guid Id { get; }
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
