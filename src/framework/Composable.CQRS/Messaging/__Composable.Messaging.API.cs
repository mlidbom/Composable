using System;
// ReSharper disable UnusedTypeParameter

namespace Composable.Messaging
{
    ///<summary>An object that is used to transfer data from a sender to a receiver through a messaging infrastructure.</summary>
    public interface IMessage {}

    ///<summary>An <see cref="IMessage"/> which informs the receiver that something has happened.
    /// <para>Should be immutable since it is impossible to change something that has already happened.</para>
    /// </summary>
    public interface IEvent : IMessage { }

    ///<summary>An <see cref="IMessage"/> that instructs the receiver to perform an action.
    /// <para>Implementations should be named as an imperative sentence with an optional(but standardized "Command" suffix): RegisterUserAccount[Command]</para></summary>
    public interface ICommand : IMessage { }

    public interface IQuery : IMessage { }

    ///<summary>An <see cref="IMessage"/> that instructs the receiver to return a resource based upon the data in the query.</summary>
    public interface IQuery<TResult> : IQuery { }


    ///<summary>The bus guarantees that any instance of a message implementing this interface will be delivered to receivers with transactional exactly-once semantics.</summary>
    public interface IExactlyOnceDeliveryMessage : IMessage
    {
        Guid MessageId { get; }
    }

    public interface IDomainCommand : ICommand, IExactlyOnceDeliveryMessage { }
    public interface IDomainCommand<TResult> : IDomainCommand { }


    public interface IDomainEvent : IEvent, IExactlyOnceDeliveryMessage
    {
        Guid EventId { get; }
        int AggregateRootVersion { get; }
        Guid AggregateRootId { get; }
        DateTime UtcTimeStamp { get; }
    }


    interface IEntityQuery<TEntity> : IQuery<TEntity>
    {
        Guid Id { get; }
    }
}
