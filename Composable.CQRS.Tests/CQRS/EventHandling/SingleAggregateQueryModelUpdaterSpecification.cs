using System;
using Composable.CQRS.EventHandling;
using Composable.CQRS.EventSourcing;
using Composable.DDD;
using Composable.KeyValueStorage;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CQRS.Tests.CQRS.EventHandling
{
    public class SingleAggregateQueryModelUpdaterSpecification : NSpec.NUnit.nspec
    {
        private Mock<IDocumentDbSession> _sessionMock;
        private UserQueryModelUpdater _listener;
        private readonly Guid _userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private UserQueryModel _queryModel;

        public void before_each()
        {
            _sessionMock = new Mock<IDocumentDbSession>(MockBehavior.Strict);
            _listener = new UserQueryModelUpdater(_sessionMock.Object);
        }

        public void after_each() {}

        public void when_listening_for_IUserEvent()
        {
            it["model is null"] = () => it["Model is not null in updater"] = () => _listener.TheModel.Should().BeNull();

            context["when receiving a IUserCreated"] =
                () =>
                {
                    before = () =>
                             {
                                 _sessionMock.Setup(session => session.Save(It.IsAny<UserQueryModel>())).Callback<UserQueryModel>(saved => _queryModel = saved);
                                 _listener.Handle(new UserCreatedEvent(_userId));
                             };

                    it["BeforeHandlers1 should be called first"] = () => _listener.BeforeHandlers1CallOrder.Should().Be(1);
                    it["BeforeHandlers2 should be called second"] = () => _listener.BeforeHandlers2CallOrder.Should().Be(2);
                    it["usercreated is called third"] = () => _listener.UserCreatedCallOrder.Should().Be(3);
                    it["AfterHandlers1 should be called fourth"] = () => _listener.AfterHandlers1CallOrder.Should().Be(4);
                    it["AftereHandlers2 should be called fifth"] = () => _listener.AfterHandlers2CallOrder.Should().Be(5);
                    it["5 calls should have been made"] = () => _listener.CallsMade.Should().Be(5);
                    it["userregistered should not be called"] = () => _listener.UserRegisteredCallOrder.Should().Be(null);
                    it["save is called on the session"] = () => _sessionMock.Verify(session => session.Save(It.IsAny<UserQueryModel>()));
                    it["Model is not null in updater"] = () => _listener.TheModel.Should().NotBeNull();
                };

            context["when receiving a IUserRegisteredEvent"] =
                () =>
                {
                    before = () =>
                             {
                                 _sessionMock.Setup(session => session.Save(It.IsAny<UserQueryModel>())).Callback<UserQueryModel>(saved => _queryModel = saved);
                                 _listener.Handle(new UserRegistered(_userId));
                             };
                    it["BeforeHandlers1 should be called first"] = () => _listener.BeforeHandlers1CallOrder.Should().Be(1);
                    it["BeforeHandlers2 should be called second"] = () => _listener.BeforeHandlers2CallOrder.Should().Be(2);
                    it["usercreated should be called third"] = () => _listener.UserCreatedCallOrder.Should().Be(3);
                    it["userregistered should be called fourth"] = () => _listener.UserRegisteredCallOrder.Should().Be(4);
                    it["AfterHandlers1 should be called fifth"] = () => _listener.AfterHandlers1CallOrder.Should().Be(5);
                    it["AftereHandlers2 should be called sixth"] = () => _listener.AfterHandlers2CallOrder.Should().Be(6);
                    it["6 calls should have been made"] = () => _listener.CallsMade.Should().Be(6);
                    it["save is called on the session"] = () => _sessionMock.Verify(session => session.Save(It.IsAny<UserQueryModel>()));
                    it["Model is not null in updater"] = () => _listener.TheModel.Should().NotBeNull();
                };

            context["when receiving a UserEditedSkills event"] =
                () =>
                {
                    before = () =>
                             {
                                 _sessionMock.Setup(session => session.GetForUpdate<UserQueryModel>(_userId)).Returns(_queryModel);
                                 _sessionMock.Setup(session => session.SaveChanges());
                                 _listener.Handle(new UserEditedSkill(_userId));
                             };
                    it["BeforeHandlers1 should be called first"] = () => _listener.BeforeHandlers1CallOrder.Should().Be(1);
                    it["BeforeHandlers2 should be called second"] = () => _listener.BeforeHandlers2CallOrder.Should().Be(2);
                    it["SkillsRemoved should be called third"] = () => _listener.SkillsRemovedCallOrder.Should().Be(3);
                    it["SkillsAdded should be called fourth"] = () => _listener.SkillsAddedCallOrder.Should().Be(4);
                    it["AfterHandlers1 should be called fifth"] = () => _listener.AfterHandlers1CallOrder.Should().Be(5);
                    it["AftereHandlers2 should be called sixth"] = () => _listener.AfterHandlers2CallOrder.Should().Be(6);
                    it["6 calls should have been made"] = () => _listener.CallsMade.Should().Be(6);
                    it["GetForUpdate is called on the session"] = () => _sessionMock.Verify(session => session.GetForUpdate<UserQueryModel>(_userId));
                    it["Model is not null in updater"] = () => _listener.TheModel.Should().NotBeNull();
                };

            context["when receiving IUserDeletedEvent"] =
                () =>
                {
                    before = () =>
                             {
                                 _sessionMock.Setup(mock => mock.Delete<UserQueryModel>(_userId));
                                 _sessionMock.Setup(mock => mock.SaveChanges());
                                 _listener.Handle(new UserDeleted(_userId));
                             };
                    it["delete is called on the session"] = () => _sessionMock.Verify(session => session.Delete<UserQueryModel>(_userId));
                    it["delete is called on the session"] = () => _sessionMock.Verify(session => session.SaveChanges());
                    it["Model is null in updater"] = () => _listener.TheModel.Should().BeNull();
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
            it["throws DuplicateRegistrationAttemptedException"] = () => Assert.Throws<DuplicateHandlerRegistrationAttemptedException>(() => new RegisterUserRegisteredTwice());
        }

        public class RegisterUserRegisteredTwice : CallsMatchingHandlersInRegistrationOrderEventHandler<RegisterUserRegisteredTwice, IUserEvent>
        {
            public RegisterUserRegisteredTwice()
            {
                RegisterHandlers()
                    .For<IUserRegistered>(e => { })
                    .For<IUserRegistered>(e => { });
            }
        }


        public class UserQueryModel : ISingleAggregateQueryModel
        {
            public Guid Id { get; set; }
            public void SetId(Guid id)
            {
                Id = id;
            }
        }

        public class UserQueryModelUpdater : SingleAggregateQueryModelUpdater<UserQueryModelUpdater, UserQueryModel, IUserEvent, IDocumentDbSession>
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

            public UserQueryModel TheModel { get { return Model; } }

            public UserQueryModelUpdater(IDocumentDbSession session)
                : base(session)
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

        public interface IUserEvent : IAggregateRootEvent {}

        public interface IUserCreatedEvent : IUserEvent, IAggregateRootCreatedEvent {}

        public interface IUserRegistered : IUserCreatedEvent {}

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

        public class UserDeleted : AggregateRootEvent, IUserDeleted {
            public UserDeleted(Guid userId):base(userId)
            {                
            }
        }

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
