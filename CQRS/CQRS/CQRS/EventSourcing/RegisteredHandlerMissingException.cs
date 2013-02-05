using System;
using Composable.System;

namespace Composable.CQRS.EventSourcing
{
    public class RegisteredHandlerMissingException : Exception
    {
        public RegisteredHandlerMissingException(Type handlerType, IAggregateRootEvent evt)
            : base(
                // ReSharper disable FormatStringProblem
            @"{0} does not have a registered handler action for incoming event {1}. " +
            "Add one in the constructor for the AggregateRootEntity by calling " +
            "Register(Handler.For<{1}>().OnApply(e => {{}})".FormatWith(handlerType, evt.GetType()))
                // ReSharper restore FormatStringProblem
        {

        }
    }

    public class HandlerRegistration
    {
        public Type EventType { get; set; }
        public Action<IAggregateRootEvent> Handler { get; set; }
    }

    public class HandlerRegistrationFor<TEvent>
        where TEvent : IAggregateRootEvent
    {
        public HandlerRegistration OnApply(Action<TEvent> action)
        {
            return new HandlerRegistration()
            {
                EventType = typeof(TEvent),
                Handler = (e) => action((TEvent)e)
            };
        }
    }

    public static class Handler
    {
        public static HandlerRegistrationFor<TEvent> For<TEvent>()
            where TEvent : IAggregateRootEvent
        {
            return new HandlerRegistrationFor<TEvent>();
        }
    }
}
