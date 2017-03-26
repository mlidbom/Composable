using System;
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedMember.Global

namespace Composable.Messaging
{
    ///<summary>An object that is used to transfer data from a sender to a receiver through a messaging infrastructure.</summary>
    public interface IMessage {}

    ///<summary>An <see cref="IMessage"/> that instructs the receiver to perform an action.
    /// <para>Implementations should be named as an imperative sentence with an optional(but standardized "Command" suffix): RegisterUserAccount[Command]</para></summary>
    public interface ICommand : IMessage
    {
        Guid Id { get; }
    }

    public interface ICommand<TResult> : ICommand where TResult : IMessage
    {
    }

    ///<summary>An <see cref="IMessage"/> which informs the receiver that something has happened.
    /// <para>Should be immutable since it is impossible to change something that has already happened.</para>
    /// </summary>
    public interface IEvent : IMessage {}


    ///<summary>An <see cref="IMessage"/> that instructs the receiver to return some data.</summary>
    public interface IQuery<TResult> : IMessage where TResult : IQueryResult {}

    ///<summary>Represent an entity within the domain of the current API that is uniquely identifiable through a query.</summary>
    public interface IResource<TResource> : IQueryResult where TResource : IResource<TResource>
    {
        IQuery<TResource> Self { get; }
    }

    ///<summary>A response to an <see cref="IQuery{TResult}"/></summary>
    public interface IQueryResult : IMessage {}

    ///<summary>Any type that subscribes to an event should implement this interface. Regardless of wether the event was Published or Replayed.</summary>
    public interface IEventSubscriber<in TEvent> where TEvent : IEvent
    {
        void Handle(TEvent message);
    }


    public class SingletonQuery<TSingleton> : IQuery<TSingleton> where TSingleton : IResource<TSingleton>
    {}

    interface IEntityQuery<TEntity> : IQuery<TEntity> where TEntity : IQueryResult
    {
        Guid Id { get; }
    }

    class EntityQuery<TEntity> : IEntityQuery<TEntity> where TEntity : IResource<TEntity>
    {
        public EntityQuery(Guid id) => Id = id;
        public Guid Id { get; }
    }

    public interface IEntityResource<TResource> : IResource<TResource> where TResource : IEntityResource<TResource>
    {
        Guid Id { get; }
    }

    public abstract class EntityResource<TResource> : IEntityResource<TResource> where TResource : EntityResource<TResource>
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
