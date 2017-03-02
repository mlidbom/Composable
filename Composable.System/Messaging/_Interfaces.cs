using System;

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

    ///<summary>An <see cref="IMessage"/> which informs the receiver that something has happened.
    /// <para>Should be immutable since it is impossible to change something that has already happened.</para>
    /// </summary>
    public interface IEvent : IMessage {}

    ///<summary>An <see cref="IMessage"/> that instructs the receiver to return some data.</summary>
    public interface IQuery<TResult> : IMessage {}

    ///<summary>Performs the action requested by a command.
    /// <para>Should be named as: (CommandName)Handler</para>
    /// </summary>
    public interface ICommandHandler<in TCommand>
    {
        void Execute(TCommand command);
    }

    ///<summary>Any type that subscribes to an event should implement this interface. Regardless of wether the event was Published or Replayed.</summary>
    public interface IEventSubscriber<in TEvent> where TEvent : IEvent
    {
        void Handle(TEvent message);
    }

    ///<summary>An <see cref="IEventSubscriber{TEvent}"/> that should be invoked when an event is initially published. </summary>
    public interface IPublishedEventSubscriber<in TEvent> : IEventSubscriber<TEvent>
        where TEvent : IEvent {}

    ///<summary>An <see cref="IEventSubscriber{TEvent}"/> that should be invoked when an event is replayed.</summary>
    public interface IReplayedEventSubscriber<in TEvent> : IEventSubscriber<TEvent> where TEvent : IEvent {}

    ///<summary>An <see cref="IEventSubscriber{TEvent}" /> that should be invoked when an event is either published or replayed.</summary>
    public interface IPublishedOrReplayedEventSubscriber<in TEvent> : IEventSubscriber<TEvent> where TEvent : IEvent {}
}
