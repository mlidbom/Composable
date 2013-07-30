using System;
using Composable.CQRS.EventHandling;
using Composable.CQRS.EventSourcing;
using Composable.DomainEvents;
using Composable.System;

namespace Composable.StuffThatDoesNotBelongHere
{
    [Obsolete("Please use UniqueMatchEventHandler to make the semantics clear. No change except changing the class you inherit is necessary")]
    public class MultiEventHandler<TImplementor, TEvent> : UniqueMatchEventHierarchyHandler<TImplementor, TEvent> 
        where TEvent : IAggregateRootEvent
        where TImplementor : MultiEventHandler<TImplementor, TEvent>
    {}

    public class AmbigousHandlerException : Exception
    {
        public AmbigousHandlerException(IDomainEvent evt) : base(evt.GetType().AssemblyQualifiedName)
        {
            
        }
    }

    public class EventUnhandledException : Exception
    {
        public EventUnhandledException(Type handlerType, IAggregateRootEvent evt, Type listenedFor)
            : base(
  @"{0} does not handle nor ignore incoming event {1} matching listened for type {2}
It should either listen for more specific events or call IgnoreUnHandled".FormatWith(handlerType, evt.GetType(), listenedFor))
        {
            
        }
    }
}