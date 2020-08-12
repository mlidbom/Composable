using Composable.Messaging;
using Composable.Messaging.Events;
using Composable.Persistence.EventStore;
using NUnit.Framework;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE0051 //Review OK: unused private members are intentional in this test.
#pragma warning disable IDE1006 //Review OK: Test Naming Styles

namespace Composable.Tests.CQRS.EventHandling
{
    public class CallMatchingHandlersInRegistrationOrderEventDispatcher_WrappedEventsTests
    {
        CallMatchingHandlersInRegistrationOrderEventDispatcher<MessageTypes.IEvent> _dispatcher;

        [SetUp] public void SetupTask() => _dispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<MessageTypes.IEvent>();

        public class Publishing_UserCreatedEvent : CallMatchingHandlersInRegistrationOrderEventDispatcher_WrappedEventsTests
        {
            EventDispatcherAsserter.RouteAssertion<MessageTypes.IEvent> AssertUserCreatedEvent() => _dispatcher.Assert().Event(new UserCreatedEvent());

            public class Dispatches_to_handler_for : Publishing_UserCreatedEvent
            {
                [Test] public void _IWrapperEvent_of_IEvent() => AssertUserCreatedEvent().DispatchesToWrapped<MessageTypes.IWrapperEvent<MessageTypes.IEvent>>();
                [Test] public void _IWrapperEvent_of_IUserEvent() => AssertUserCreatedEvent().DispatchesToWrapped<MessageTypes.IWrapperEvent<IUserEvent>>();
                [Test] public void _IWrapperEvent_of_IUserCreatedEvent() => AssertUserCreatedEvent().DispatchesToWrapped<MessageTypes.IWrapperEvent<IUserCreatedEvent>>();
            }

            public class Does_not_dispatch_to_handler_for : Publishing_UserCreatedEvent
            {
                [Test] public void _IUserWrapperEvent_of_IUserEvent() => AssertUserCreatedEvent().DoesNotDispatchToWrapped<IUserWrapperEvent<IUserEvent>>();
            }
        }

        public class Publishing_WrapperEvent_of_UserCreatedEvent : CallMatchingHandlersInRegistrationOrderEventDispatcher_WrappedEventsTests
        {
            EventDispatcherAsserter.RouteAssertion<MessageTypes.IEvent> AssertUserCreatedEvent() => _dispatcher.Assert().Event(new MessageTypes.WrapperEvent<UserCreatedEvent>(new UserCreatedEvent()));

            public class Dispatches_to_handler_for : Publishing_WrapperEvent_of_UserCreatedEvent
            {
                [Test] public void _IWrapperEvent_of_IEvent() => AssertUserCreatedEvent().DispatchesToWrapped<MessageTypes.IWrapperEvent<MessageTypes.IEvent>>();
                [Test] public void _IWrapperEvent_of_IUserEvent() => AssertUserCreatedEvent().DispatchesToWrapped<MessageTypes.IWrapperEvent<IUserEvent>>();
                [Test] public void _IWrapperEvent_of_IUserCreatedEvent() => AssertUserCreatedEvent().DispatchesToWrapped<MessageTypes.IWrapperEvent<IUserCreatedEvent>>();
            }

            public class Does_not_dispatch_to_handler_for : Publishing_WrapperEvent_of_UserCreatedEvent
            {
                [Test] public void _IUserWrapperEvent_of_IUserEvent() => AssertUserCreatedEvent().DoesNotDispatchToWrapped<IUserWrapperEvent<IUserEvent>>();
            }
        }

        public class Publishing_UserWrapperEvent_of_UserCreatedEvent : CallMatchingHandlersInRegistrationOrderEventDispatcher_WrappedEventsTests
        {
            EventDispatcherAsserter.RouteAssertion<MessageTypes.IEvent> AssertUserWrapperEvent_of_UserCreatedEvent() => _dispatcher.Assert().Event(new UserWrapperEvent<UserCreatedEvent>(new UserCreatedEvent()));

            public class Dispatches_to_handler_for : Publishing_UserWrapperEvent_of_UserCreatedEvent
            {
                [Test] public void _IWrapperEvent_of_IEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<MessageTypes.IWrapperEvent<MessageTypes.IEvent>>();
                [Test] public void _IWrapperEvent_of_IUserEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<MessageTypes.IWrapperEvent<IUserEvent>>();
                [Test] public void _IWrapperEvent_of_IUserCreatedEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<MessageTypes.IWrapperEvent<IUserCreatedEvent>>();
                [Test] public void _IUserWrapperEvent_of_IUserEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IUserWrapperEvent<IUserEvent>>();
            }

            public class Does_not_dispatch_to_handler_for : Publishing_UserWrapperEvent_of_UserCreatedEvent
            {
                [Test] public void _IAdminUserWrapperEvent_of_IAdminUserEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DoesNotDispatchToWrapped<IAdminUserWrapperEvent<IAdminUserEvent>>();
            }
        }

        interface IUserWrapperEvent<out TEvent> : MessageTypes.IWrapperEvent<TEvent> where TEvent : IUserEvent {}
        class UserWrapperEvent<TEvent> : MessageTypes.WrapperEvent<TEvent>, IUserWrapperEvent<TEvent> where TEvent : IUserEvent
        {
            public UserWrapperEvent(TEvent @event) : base(@event) {}
        }

        interface IAdminUserWrapperEvent<out TEvent> : IUserWrapperEvent<TEvent> where TEvent : IAdminUserEvent {}

        class AdminUserWrapperEvent<TEvent> : UserWrapperEvent<TEvent>, IAdminUserWrapperEvent<TEvent> where TEvent : IAdminUserEvent
        {
            public AdminUserWrapperEvent(TEvent @event) : base(@event) {}
        }

        interface IUserEvent : IAggregateEvent {}
        interface IUserCreatedEvent : IUserEvent {}
        interface IUserRegistered : IUserCreatedEvent {}
        interface IUserSkillsEvent : IUserEvent {}
        interface IUserSkillsAdded : IUserSkillsEvent {}
        interface IUserSkillsRemoved : IUserSkillsEvent {}
        interface IIgnoredUserEvent : IUserEvent {}

        class UnHandledUserEvent : AggregateEvent, IUserEvent {}

        class IgnoredUserEvent : AggregateEvent, IIgnoredUserEvent {}

        class UserCreatedEvent : AggregateEvent, IUserCreatedEvent {}

        class UserRegistered : AggregateEvent, IUserRegistered {}

        interface IAdminUserEvent : IUserEvent {}
    }
}
