using System;
using Composable.CQRS.EventHandling;
using Composable.CQRS.EventSourcing;
using Composable.DomainEvents;
using Composable.System;

namespace Composable.StuffThatDoesNotBelongHere
{
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