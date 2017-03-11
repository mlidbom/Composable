using System;

namespace Composable.Messaging
{
    ///<summary>An object that is used to transfer data from a sender to a receiver through a messaging infrastructure.</summary>
    public interface IMessage {}

    ///<summary>An <see cref="IMessage"/> that instructs the receiver to perform an action.
    /// <para>Implementations should be named as an imperative sentence with an optional(but standardized "Command" suffix): RegisterUserAccount[Command]</para></summary>
    public interface ICommand : IMessage
    {
        // ReSharper disable once UnusedMemberInSuper.Global
        Guid Id { get; }
    }

    ///<summary>An <see cref="IMessage"/> which informs the receiver that something has happened.
    /// <para>Should be immutable since it is impossible to change something that has already happened.</para>
    /// </summary>
    public interface IEvent : IMessage {}


    ///<summary>An <see cref="IMessage"/> that instructs the receiver to return some data.</summary>
    // ReSharper disable once UnusedTypeParameter
    public interface IQuery<TResult> : IMessage where TResult : IQueryResult {}

    ///<summary>A response to an <see cref="IQuery{TResult}"/></summary>
    public interface IQueryResult : IMessage {}

    ///<summary>Any type that subscribes to an event should implement this interface. Regardless of wether the event was Published or Replayed.</summary>
    public interface IEventSubscriber<in TEvent> where TEvent : IEvent
    {
        void Handle(TEvent message);
    }
}
