using Composable.CQRS.EventHandling;
using Composable.CQRS.EventSourcing;
using FluentAssertions;
using NUnit.Framework;

namespace CQRS.Tests.CQRS.EventHandling
{
    public partial class MultiEventHandlerSpecification : NSpec.NUnit.nspec
    {
        public void when_listening_for_IUserEvent()
        {
            HandlesIUserEvents listener = null;
            before = () => listener = new HandlesIUserEvents();

            context["when receiving a IUserCreated"] =
                () =>
                {
                    before = () => listener.Handle(new UserCreatedEvent());
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
                    before = () => listener.Handle(new UserRegistered());
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
                    before = () => listener.Handle(new UserEditedSkills());
                    it["BeforeHandlers1 should be called first"] = () => listener.BeforeHandlers1CallOrder.Should().Be(1);
                    it["BeforeHandlers2 should be called second"] = () => listener.BeforeHandlers2CallOrder.Should().Be(2);
                    it["SkillRemoved should be called third"] = () => listener.SkillRemovedCallOrder.Should().Be(3);
                    it["SkillAdded should be called fourth"] = () => listener.SkillAddedCallOrder.Should().Be(4);
                    it["AfterHandlers1 should be called fifth"] = () => listener.AfterHandlers1CallOrder.Should().Be(5);
                    it["AftereHandlers2 should be called sixth"] = () => listener.AfterHandlers2CallOrder.Should().Be(6);
                    it["6 calls should have been made"] = () => listener.CallsMade.Should().Be(6);
                };
        }

        public void when_attempting_to_register_event_twice()
        {
            it["throws DuplicateRegistrationAttemptedException"] = () => Assert.Throws<DuplicateHandlerRegistrationAttemptedException>(() => new RegisterUserRegisteredTwice());
        }
    }


    public partial class MultiEventHandlerSpecification
    {
        public class RegisterUserRegisteredTwice : MultiEventHandler<RegisterUserRegisteredTwice, IUserEvent>
        {
            public RegisterUserRegisteredTwice()
            {
                RegisterHandlers()
                    .For<IUserRegistered>(e => { })
                    .For<IUserRegistered>(e => { });
            }
        }

        public class HandlesIUserEvents : MultiEventHandler<HandlesIUserEvents, IUserEvent>
        {
            public int CallsMade = 0;

            public int? BeforeHandlers1CallOrder;
            public int? BeforeHandlers2CallOrder;

            public int? UserCreatedCallOrder;
            public int? UserRegisteredCallOrder;
            public int? SkillAddedCallOrder;
            public int? SkillRemovedCallOrder;


            public int? AfterHandlers1CallOrder;
            public int? AfterHandlers2CallOrder;

            public HandlesIUserEvents()
            {
                RegisterHandlers()
                    .BeforeHandlers(e => BeforeHandlers1CallOrder = ++CallsMade)
                    .BeforeHandlers(e => BeforeHandlers2CallOrder = ++CallsMade)
                    .AfterHandlers(e => AfterHandlers1CallOrder = ++CallsMade)
                    .AfterHandlers(e => AfterHandlers2CallOrder = ++CallsMade)
                    .For<IUserCreatedEvent>(e => UserCreatedCallOrder = ++CallsMade)
                    .For<IUserRegistered>(e => UserRegisteredCallOrder = ++CallsMade)
                    .For<IUserSkillRemoved>(e => SkillRemovedCallOrder = ++CallsMade)
                    .For<IUserSkillAdded>(e => SkillAddedCallOrder = ++CallsMade);
            }
        }

        public interface IUserEvent : IAggregateRootEvent {}

        public interface IUserCreatedEvent : IUserEvent {}

        private class UserCreatedEvent : AggregateRootEvent, IUserCreatedEvent {}

        public interface IUserRegistered : IUserCreatedEvent, IAggregateRootCreatedEvent
        {
            string Email { get; set; }
            string Password { get; set; }
        }

        public interface IUserSkillEvent : MultiEventHandlerSpecification.IUserEvent {}

        public interface IUserSkillAdded : IUserSkillEvent {}

        public interface IUserSkillRemoved : IUserSkillEvent {}

        public interface IUserAddedSkill : IUserSkillAdded {}

        public interface IUserRemovedSkill : IUserSkillRemoved {}

        public interface IUserEditedSkills : IUserAddedSkill, IUserRemovedSkill {}

        public class UserEditedSkills : AggregateRootEvent, IUserEditedSkills {}


        public interface IUserDeleted : IUserEvent, IAggregateRootDeletedEvent {}

        private class UserDeleted : AggregateRootEvent, IUserDeleted {}

        public class UserRegistered : AggregateRootEvent, IUserRegistered
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }
    }
}
