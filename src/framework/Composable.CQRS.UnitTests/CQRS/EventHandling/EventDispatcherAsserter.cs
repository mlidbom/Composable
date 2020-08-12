using Composable.Messaging;
using Composable.Messaging.Events;
using FluentAssertions;

namespace Composable.Tests.CQRS.EventHandling
{
    static class EventDispatcherAsserter
    {
        internal class DispatcherAssertion<TDispatcherRootEvent> where TDispatcherRootEvent : class, MessageTypes.IEvent
        {
            readonly IMutableEventDispatcher<TDispatcherRootEvent> _dispatcher;
            public DispatcherAssertion(IMutableEventDispatcher<TDispatcherRootEvent> dispatcher) => _dispatcher = dispatcher;

            public RouteAssertion<TDispatcherRootEvent> Event<TPublishedEvent>(TPublishedEvent @event)
                where TPublishedEvent : TDispatcherRootEvent =>
                new RouteAssertion<TDispatcherRootEvent>(_dispatcher, @event);
        }

        internal class RouteAssertion<TDispatcherRootEvent> where TDispatcherRootEvent : class, MessageTypes.IEvent
        {
            readonly IMutableEventDispatcher<TDispatcherRootEvent> _dispatcher;
            readonly TDispatcherRootEvent _event;
            public RouteAssertion(IMutableEventDispatcher<TDispatcherRootEvent> dispatcher, TDispatcherRootEvent @event)
            {
                _dispatcher = dispatcher;
                _event = @event;
            }

            public void DispatchesTo<THandlerEvent>()
                where THandlerEvent : TDispatcherRootEvent
            {
                var callCount = 0;
                _dispatcher.Register().IgnoreAllUnhandled();
                _dispatcher.Register().For((THandlerEvent @event) => callCount++);
                _dispatcher.Dispatch(_event);
                callCount.Should().Be(1, "Message was not dispatched to handler.");
            }

            public void DispatchesToWrapped<THandlerEvent>()
                where THandlerEvent : MessageTypes.IWrapperEvent<TDispatcherRootEvent>
            {
                var callCount = 0;
                _dispatcher.Register().IgnoreAllUnhandled();
                _dispatcher.Register().ForWrapped((THandlerEvent @event) => callCount++);
                _dispatcher.Dispatch(_event);
                callCount.Should().Be(1, "Message was not dispatched to handler.");
            }

            public void DoesNotDispatchToWrapped<THandlerEvent>()
                where THandlerEvent : MessageTypes.IWrapperEvent<TDispatcherRootEvent>
            {
                var callCount = 0;
                _dispatcher.Register().IgnoreAllUnhandled();
                _dispatcher.Register().ForWrapped((THandlerEvent @event) => callCount++);
                _dispatcher.Dispatch(_event);
                callCount.Should().Be(0, "Message was dispatched to handler.");
            }
        }

        internal static DispatcherAssertion<TEvent> Assert<TEvent>(this IMutableEventDispatcher<TEvent> @this) where TEvent : class, MessageTypes.IEvent => new DispatcherAssertion<TEvent>(@this);
    }
}
