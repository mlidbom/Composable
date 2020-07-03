﻿using Composable.Messaging.Events;
using FluentAssertions;
using Xunit;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE0051 //Review OK: unused private members are intentional in this test.
#pragma warning disable IDE1006 //Review OK: Test Naming Styles

namespace Composable.Tests.CQRS.EventHandling
{
    public abstract class CallMatchingHandlersInRegistrationOrderEventDispatcherSpecification
    {
        public class Given_an_instance
        {
            readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<IUserEvent> _dispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<IUserEvent>();

            public class with_2_BeforeHandlers_2_AfterHandlers_and_1_handler_each_per_4_specific_event_type : Given_an_instance
            {
                int CallsMade { get; set; }

                int? BeforeHandlers1CallOrder { get; set; }
                int? BeforeHandlers2CallOrder { get; set; }

                int? UserCreatedCallOrder { get; set; }

                int? AfterHandlers1CallOrder { get; set; }
                int? AfterHandlers2CallOrder { get; set; }

                public with_2_BeforeHandlers_2_AfterHandlers_and_1_handler_each_per_4_specific_event_type()
                {
                    _dispatcher.Register()
                               .IgnoreUnhandled<IIgnoredUserEvent>()
                               .BeforeHandlers(e => BeforeHandlers1CallOrder = ++CallsMade)
                               .BeforeHandlers(e => BeforeHandlers2CallOrder = ++CallsMade)
                               .AfterHandlers(e => AfterHandlers1CallOrder = ++CallsMade)
                               .AfterHandlers(e => AfterHandlers2CallOrder = ++CallsMade)
                               .For<IUserCreatedEvent>(e => UserCreatedCallOrder = ++CallsMade)
                               .For<IUserRegistered>(e => ++CallsMade)
                               .For<IUserSkillsRemoved>(e => ++CallsMade)
                               .For<IUserSkillsAdded>(e => ++CallsMade);
                }

                [Fact] void when_dispatching_an_ignored_event_no_calls_are_made_to_any_handlers()
                {
                    _dispatcher.Dispatch(new IgnoredUserEvent());
                    CallsMade.Should().Be(0);
                }

                [Fact] void when_dispatching_an_unhandled_event_that_is_not_ignored_an_exception_is_thrown() =>
                    Assert.ThrowsAny<EventUnhandledException>(() => _dispatcher.Dispatch(new UnHandledUserEvent()));

                public class when_dispatching_an_IUserCreatedEvent : with_2_BeforeHandlers_2_AfterHandlers_and_1_handler_each_per_4_specific_event_type
                {
                    public when_dispatching_an_IUserCreatedEvent() { _dispatcher.Dispatch(new UserCreatedEvent()); }

                    [Fact] void BeforeHandler1_is_called_first() => BeforeHandlers1CallOrder.Should().Be(1);
                    [Fact] void BeforeHandler2_is_called_second() => BeforeHandlers2CallOrder.Should().Be(2);
                    [Fact] void The_specific_handler_is_called_third() => UserCreatedCallOrder.Should().Be(3);
                    [Fact] void AfterHandler1_is_called_fourth() => AfterHandlers1CallOrder.Should().Be(4);
                    [Fact] void AfterHandler2_is_called_fifth() => AfterHandlers2CallOrder.Should().Be(5);
                    [Fact] void Five_calls_are_made_in_total() => CallsMade.Should().Be(5);
                }
            }

            public class with_2_registered_handlers_for_the_same_event_type_then_when_dispatching_event : Given_an_instance
            {
                [Fact] void handlers_are_called_in_registration_order()
                {
                    var calls = 0;
                    var handler1CallOrder = 0;
                    var handler2CallOrder = 0;

                    _dispatcher.RegisterHandlers()
                               .For<IUserRegistered>(e => handler1CallOrder = ++calls)
                               .For<IUserRegistered>(e => handler2CallOrder = ++calls);

                    _dispatcher.Dispatch(new UserRegistered());

                    handler1CallOrder.Should().Be(1);
                    handler2CallOrder.Should().Be(2);
                }
            }

            interface IUserEvent {}
            interface IUserCreatedEvent : IUserEvent {}
            interface IUserRegistered : IUserCreatedEvent{}
            interface IUserSkillsEvent : IUserEvent {}
            interface IUserSkillsAdded : IUserSkillsEvent {}
            interface IUserSkillsRemoved : IUserSkillsEvent {}
            interface IIgnoredUserEvent : IUserEvent {}

            class UnHandledUserEvent : IUserEvent {}

            class IgnoredUserEvent : IIgnoredUserEvent {}

            class UserCreatedEvent : IUserCreatedEvent
            {}

            class UserRegistered : IUserRegistered
            {}
        }
    }
}
