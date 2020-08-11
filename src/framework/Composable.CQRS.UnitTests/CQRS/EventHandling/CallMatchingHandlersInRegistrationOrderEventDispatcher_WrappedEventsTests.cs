using Composable.Messaging;
using Composable.Messaging.Events;
using Composable.Persistence.EventStore;
using FluentAssertions;
using NUnit.Framework;
using Xunit;
using Assert = Xunit.Assert;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE0051 //Review OK: unused private members are intentional in this test.
#pragma warning disable IDE1006 //Review OK: Test Naming Styles

namespace Composable.Tests.CQRS.EventHandling
{
    public abstract class CallMatchingHandlersInRegistrationOrderEventDispatcher_WrappedEventsTests
    {
        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<IUserEvent> _dispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<IUserEvent>();

        [Test] public void Dispatches_to_wrapped_event_handler_when_publishing_unwrapped()
        {
            int called = 0;
            _dispatcher.Register().ForWrapped((MessageTypes.IWrapperEvent<IUserCreatedEvent> @event) => called++);

            _dispatcher.Dispatch(new UserCreatedEvent());

            called.Should().Be(1);
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
    }
}
