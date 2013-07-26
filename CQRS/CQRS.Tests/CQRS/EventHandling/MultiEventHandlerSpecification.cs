using System;
using Composable.CQRS.EventHandling;
using Composable.CQRS.EventSourcing;
using Composable.DDD;
using Composable.KeyValueStorage;
using FluentAssertions;
using Moq;
using NServiceBus;
using NUnit.Framework;

namespace CQRS.Tests.CQRS.EventHandling
{
    public class MultiEventHandlerSpecification : NSpec.NUnit.nspec
    {
        public void before_each()
        {
            listener = new HandlesIUserEvents();
        }

        protected IHandlesIUserEvents listener;
        protected Guid _userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        public void when_listening_for_IUserEvent()
        {
            context["when receiving a IUserCreated"] =
                () =>
                {
                    before = () => listener.Handle(new UserCreatedEvent(_userId));
                    it["BeforeHandlers1 should be called first"] = () => listener.BeforeHandlers1CallOrder.Should().Be(1);
                    it["BeforeHandlers2 should be called second"] = () => listener.BeforeHandlers2CallOrder.Should().Be(2);
                    it["usercreated is called third"] = () => listener.UserCreatedCallOrder.Should().Be(3);
                    it["AfterHandlers1 should be called fourth"] = () => listener.AfterHandlers1CallOrder.Should().Be(4);
                    it["AftereHandlers2 should be called fifth"] = () => listener.AfterHandlers2CallOrder.Should().Be(5);
                    it["5 calls should have been made"] = () => listener.CallsMade.Should().Be(5);
                    it["userregistered should not be called"] = () => listener.UserRegisteredCallOrder.Should().Be(null);
                };

            context["when receiving a IUserRegisteredEvent"] =
                () =>
                {
                    before = () => listener.Handle(new UserRegistered(_userId));
                    it["BeforeHandlers1 should be called first"] = () => listener.BeforeHandlers1CallOrder.Should().Be(1);
                    it["BeforeHandlers2 should be called second"] = () => listener.BeforeHandlers2CallOrder.Should().Be(2);
                    it["usercreated should be called third"] = () => listener.UserCreatedCallOrder.Should().Be(3);
                    it["userregistered should be called fourth"] = () => listener.UserRegisteredCallOrder.Should().Be(4);
                    it["AfterHandlers1 should be called fifth"] = () => listener.AfterHandlers1CallOrder.Should().Be(5);
                    it["AftereHandlers2 should be called sixth"] = () => listener.AfterHandlers2CallOrder.Should().Be(6);
                    it["6 calls should have been made"] = () => listener.CallsMade.Should().Be(6);
                };

            context["when receiving a UserEditedSkills event"] =
                () =>
                {
                    before = () => listener.Handle(new UserEditedSkill(_userId));
                    it["BeforeHandlers1 should be called first"] = () => listener.BeforeHandlers1CallOrder.Should().Be(1);
                    it["BeforeHandlers2 should be called second"] = () => listener.BeforeHandlers2CallOrder.Should().Be(2);
                    it["SkillsRemoved should be called third"] = () => listener.SkillsRemovedCallOrder.Should().Be(3);
                    it["SkillsAdded should be called fourth"] = () => listener.SkillsAddedCallOrder.Should().Be(4);
                    it["AfterHandlers1 should be called fifth"] = () => listener.AfterHandlers1CallOrder.Should().Be(5);
                    it["AftereHandlers2 should be called sixth"] = () => listener.AfterHandlers2CallOrder.Should().Be(6);
                    it["6 calls should have been made"] = () => listener.CallsMade.Should().Be(6);
                };

            context["when receiving IIgnoredUserEvent"] =
                () =>
                {
                    before = () => listener.Handle(new IgnoredUserEvent());
                    it["there are 0 calls"] = () => listener.CallsMade.Should().Be(0);
                };

            context["when receiving unhandled event"] =
                () => { it["unhandled event exception is thrown"] = () => expect<EventUnhandledException>(() => listener.Handle(new UnHandledEvent())); };
        }

        public void when_attempting_to_register_event_twice()
        {
            it["throws DuplicateRegistrationAttemptedException"] = () => Assert.Throws<DuplicateHandlerRegistrationAttemptedException>(() => new RegisterUserRegisteredTwice());
        }

        public class RegisterUserRegisteredTwice : MultiEventHandler<RegisterUserRegisteredTwice, IUserEvent>
        {
            public RegisterUserRegisteredTwice()
            {
                RegisterHandlers()
                    .For<IUserRegistered>(e => { })
                    .For<IUserRegistered>(e => { });
            }
        }

        public interface IHandlesIUserEvents : IHandleMessages<IUserEvent>
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

        public class HandlesIUserEvents : MultiEventHandler<HandlesIUserEvents, IUserEvent>, IHandlesIUserEvents
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

            public HandlesIUserEvents()
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


        public class UserQueryModel : IHasPersistentIdentity<Guid>
        {
            public Guid Id { get; set; }
        }      

        public interface IUserEvent : IAggregateRootEvent {}

        public interface IUserCreatedEvent : IUserEvent {}

        public interface IUserRegistered : IUserCreatedEvent, IAggregateRootCreatedEvent {}

        public interface IUserDeleted : IUserEvent, IAggregateRootDeletedEvent {}

        public interface IUserSkillsEvent : IUserEvent {}

        public interface IUserSkillsAdded : IUserSkillsEvent {}

        public interface IUserSkillsRemoved : IUserSkillsEvent {}

        public interface IUserAddedSkills : IUserSkillsAdded {}

        public interface IUserRemovedSkills : IUserSkillsRemoved {}

        public interface IUserEditedSkill : IUserAddedSkills, IUserRemovedSkills {}

        public interface IIgnoredUserEvent : IUserEvent {}

        public class IgnoredUserEvent : AggregateRootEvent, IIgnoredUserEvent {}

        public class UserCreatedEvent : AggregateRootEvent, IUserCreatedEvent
        {
            public UserCreatedEvent(Guid userId) : base(userId) {}
        }

        public class UserDeleted : AggregateRootEvent, IUserDeleted {}

        public class UserEditedSkill : AggregateRootEvent, IUserEditedSkill
        {
            public UserEditedSkill(Guid userId) : base(userId) {}
        }

        public class UserRegistered : AggregateRootEvent, IUserRegistered
        {
            public UserRegistered(Guid userId) : base(userId) {}
        }

        public class UnHandledEvent : AggregateRootEvent, IUserEvent {}
    }
}