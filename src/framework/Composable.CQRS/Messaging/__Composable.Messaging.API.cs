using System;
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedMember.Global

namespace Composable.Messaging
{
    ///<summary>An object that is used to transfer data from a sender to a receiver through a messaging infrastructure.</summary>
    public interface IMessage {}

    public interface IExactlyOnceDeliveryMessage : IMessage
    {
        Guid MessageId { get; }
    }

    ///<summary>An <see cref="IMessage"/> that instructs the receiver to perform an action.
    /// <para>Implementations should be named as an imperative sentence with an optional(but standardized "Command" suffix): RegisterUserAccount[Command]</para></summary>
    public interface ICommand : IExactlyOnceDeliveryMessage { }

    public interface ICommand<TResult> : ICommand where TResult : IMessage { }

    ///<summary>An <see cref="IMessage"/> which informs the receiver that something has happened.
    /// <para>Should be immutable since it is impossible to change something that has already happened.</para>
    /// </summary>
    public interface IEvent : IMessage {}

    public interface IAggregateRootEvent : IEvent, IExactlyOnceDeliveryMessage
    {
        Guid EventId { get; }
        int AggregateRootVersion { get; }
        Guid AggregateRootId { get; }
        DateTime UtcTimeStamp { get; }
    }


    public interface IQuery : IMessage {}

    ///<summary>An <see cref="IMessage"/> that instructs the receiver to return some data.</summary>
    public interface IQuery<TResult> : IQuery where TResult : IQueryResult {}

    ///<summary>Represent an entity within the domain of the current API that is uniquely identifiable through a query.</summary>
    public interface IResource<TResource> : IQueryResult where TResource : IResource<TResource>
    {
        IQuery<TResource> Self { get; }
    }

    ///<summary>A response to an <see cref="IQuery{TResult}"/></summary>
    public interface IQueryResult : IMessage {}

    interface IEntityQuery<TEntity> : IQuery<TEntity> where TEntity : IQueryResult
    {
        Guid Id { get; }
    }
}
