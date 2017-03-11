using System;
using Composable.CQRS.EventSourcing;
using Composable.DDD;
using Composable.Messaging;
using Composable.Messaging.Events;
using FluentAssertions;

// ReSharper disable UnusedMember.Global

namespace Composable.CQRS.Specs.CQRS.EventHandling
{
    public class EventHierarchyHandlerSpecification : nspec
    {
        public void before_each()
        {
            _listener = new HandlesIUserEventsHierarchy();
        }

        IHandlesIUserEvents _listener;
        Guid _userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        public void when_listening_for_IUserEvent()
        {
            context["when receiving an IUserCreated"] =
                () =>
                {
                    before = () => _listener.Handle(new UserCreatedEvent(_userId));
                    it["BeforeHandlers1 should be called first"] = () => _listener.BeforeHandlers1CallOrder.Should().Be(1);
                    it["BeforeHandlers2 should be called second"] = () => _listener.BeforeHandlers2CallOrder.Should().Be(2);
                    it["usercreated is called third"] = () => _listener.UserCreatedCallOrder.Should().Be(3);
                    it["AfterHandlers1 should be called fourth"] = () => _listener.AfterHandlers1CallOrder.Should().Be(4);
                    it["AftereHandlers2 should be called fifth"] = () => _listener.AfterHandlers2CallOrder.Should().Be(5);
                    it["5 calls should have been made"] = () => _listener.CallsMade.Should().Be(5);
                    it["userregistered should not be called"] = () => _listener.UserRegisteredCallOrder.Should().Be(null);
                };

            context["when receiving a IUserRegisteredEvent"] =
                () =>
                {
                    before = () => _listener.Handle(new UserRegistered(_userId));
                    it["BeforeHandlers1 should be called first"] = () => _listener.BeforeHandlers1CallOrder.Should().Be(1);
                    it["BeforeHandlers2 should be called second"] = () => _listener.BeforeHandlers2CallOrder.Should().Be(2);
                    it["usercreated should be called third"] = () => _listener.UserCreatedCallOrder.Should().Be(3);
                    it["userregistered should be called fourth"] = () => _listener.UserRegisteredCallOrder.Should().Be(4);
                    it["AfterHandlers1 should be called fifth"] = () => _listener.AfterHandlers1CallOrder.Should().Be(5);
                    it["AftereHandlers2 should be called sixth"] = () => _listener.AfterHandlers2CallOrder.Should().Be(6);
                    it["6 calls should have been made"] = () => _listener.CallsMade.Should().Be(6);
                };

            context["when receiving a UserEditedSkills event"] =
                () =>
                {
                    before = () => _listener.Handle(new UserEditedSkill(_userId));
                    it["BeforeHandlers1 should be called first"] = () => _listener.BeforeHandlers1CallOrder.Should().Be(1);
                    it["BeforeHandlers2 should be called second"] = () => _listener.BeforeHandlers2CallOrder.Should().Be(2);
                    it["SkillsRemoved should be called third"] = () => _listener.SkillsRemovedCallOrder.Should().Be(3);
                    it["SkillsAdded should be called fourth"] = () => _listener.SkillsAddedCallOrder.Should().Be(4);
                    it["AfterHandlers1 should be called fifth"] = () => _listener.AfterHandlers1CallOrder.Should().Be(5);
                    it["AftereHandlers2 should be called sixth"] = () => _listener.AfterHandlers2CallOrder.Should().Be(6);
                    it["6 calls should have been made"] = () => _listener.CallsMade.Should().Be(6);
                };

            context["when receiving IIgnoredUserEvent"] =
                () =>
                {
                    before = () => _listener.Handle(new IgnoredUserEvent());
                    it["there are 0 calls"] = () => _listener.CallsMade.Should().Be(0);
                };

            context["when receiving unhandled event"] =
                () => { it["unhandled event exception is thrown"] = () => expect<EventUnhandledException>(() => _listener.Handle(new UnHandledEvent())); };
        }

        public void when_attempting_to_register_event_twice()
        {
            RegisterUserRegisteredTwice doubleRegistration = null;
            before = () =>
            {
                doubleRegistration = new RegisterUserRegisteredTwice();
                doubleRegistration.Handle(new UserRegistered(_userId));
            };
            it["no exception is thrown"] = () => { };
            it["First handler is called first"] = () => doubleRegistration.Handler1CallOrder.Should().Be(1);
            it["Second handler is called"] = () => doubleRegistration.Handler2CallOrder.Should().Be(2);
        }

        class RegisterUserRegisteredTwice : CallsMatchingHandlersInRegistrationOrderEventHandler<IUserEvent>
        {
            int _calls = 0;
            public int Handler1CallOrder = 0;
            public int Handler2CallOrder = 0;
            public RegisterUserRegisteredTwice()
            {
                RegisterHandlers()
                    .For<IUserRegistered>(e => Handler1CallOrder = ++_calls)
                    .For<IUserRegistered>(e => Handler2CallOrder = ++_calls);
            }
        }

        interface IHandlesIUserEvents : IEventSubscriber<IUserEvent>
        {
            int CallsMade { get; set; }
            int? BeforeHandlers1CallOrder { get; set; }
            int? BeforeHandlers2CallOrder { get; set; }
            int? UserCreatedCallOrder { get; set; }
            int? UserRegisteredCallOrder { get; set; }
            int? SkillsAddedCallOrder { get; set; }
            int? SkillsRemovedCallOrder { get; set; }
            int? AfterHandlers1CallOrder { get; set; }
            int? AfterHandlers2CallOrder { get; set; }
        }

        class HandlesIUserEventsHierarchy : CallsMatchingHandlersInRegistrationOrderEventHandler<IUserEvent>, IHandlesIUserEvents
        {
            public int CallsMade { get; set; }

            public int? BeforeHandlers1CallOrder { get; set; }
            public int? BeforeHandlers2CallOrder { get; set; }

            public int? UserCreatedCallOrder { get; set; }
            public int? UserRegisteredCallOrder { get; set; }
            public int? SkillsAddedCallOrder { get; set; }
            public int? SkillsRemovedCallOrder { get; set; }


            public int? AfterHandlers1CallOrder { get; set; }
            public int? AfterHandlers2CallOrder { get; set; }

            public HandlesIUserEventsHierarchy()
            {
                RegisterHandlers()
                    .IgnoreUnhandled<IIgnoredUserEvent>()
                    .BeforeHandlers(e => BeforeHandlers1CallOrder = ++CallsMade)
                    .BeforeHandlers(e => BeforeHandlers2CallOrder = ++CallsMade)
                    .AfterHandlers(e => AfterHandlers1CallOrder = ++CallsMade)
                    .AfterHandlers(e => AfterHandlers2CallOrder = ++CallsMade)
                    .For<IUserCreatedEvent>(e => UserCreatedCallOrder = ++CallsMade)
                    .For<IUserRegistered>(e => UserRegisteredCallOrder = ++CallsMade)
                    .For<IUserSkillsRemoved>(e => SkillsRemovedCallOrder = ++CallsMade)
                    .For<IUserSkillsAdded>(e => SkillsAddedCallOrder = ++CallsMade);
            }
        }


        interface IUserEvent : IAggregateRootEvent {}

        interface IUserCreatedEvent : IUserEvent {}

        interface IUserRegistered : IUserCreatedEvent, IAggregateRootCreatedEvent {}

        interface IUserDeleted : IUserEvent, IAggregateRootDeletedEvent {}

        interface IUserSkillsEvent : IUserEvent {}

        interface IUserSkillsAdded : IUserSkillsEvent {}

        interface IUserSkillsRemoved : IUserSkillsEvent {}

        interface IUserAddedSkills : IUserSkillsAdded {}

        interface IUserRemovedSkills : IUserSkillsRemoved {}

        interface IUserEditedSkill : IUserAddedSkills, IUserRemovedSkills {}

        interface IIgnoredUserEvent : IUserEvent {}

        class IgnoredUserEvent : AggregateRootEvent, IIgnoredUserEvent {}

        class UserCreatedEvent : AggregateRootEvent, IUserCreatedEvent
        {
            public UserCreatedEvent(Guid userId) : base(userId) {}
        }

        public class UserDeleted : AggregateRootEvent, IUserDeleted {}

        class UserEditedSkill : AggregateRootEvent, IUserEditedSkill
        {
            public UserEditedSkill(Guid userId) : base(userId) {}
        }

        class UserRegistered : AggregateRootEvent, IUserRegistered
        {
            public UserRegistered(Guid userId) : base(userId) {}
        }

        class UnHandledEvent : AggregateRootEvent, IUserEvent {}
    }
}