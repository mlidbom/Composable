using System;
using Composable.Messaging.Events;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.EventStore;
using FluentAssertions;
using Moq;

// ReSharper disable UnusedMember.Global

namespace Composable.CQRS.Tests.CQRS.EventHandling
{
    public class SingleAggregateQueryModelUpdaterSpecification : nspec
    {
        Mock<IDocumentDbSession> _sessionMock;
        UserQueryModelUpdater _listener;
        readonly Guid _userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        UserQueryModel _queryModel;

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
                () => it["unhandled event exception is thrown"] = () => expect<EventUnhandledException>(() => _listener.Handle(new UnHandledEvent()));
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


        class UserQueryModel : ISingleAggregateQueryModel
        {
            public Guid Id { get; private set; }
            public void SetId(Guid id)
            {
                Id = id;
            }
        }

        class UserQueryModelUpdater : SingleAggregateQueryModelUpdater<UserQueryModelUpdater, UserQueryModel, IUserEvent, IDocumentDbSession>
        {
            public int CallsMade { get; private set; }

            public int? BeforeHandlers1CallOrder { get; private set; }
            public int? BeforeHandlers2CallOrder { get; private set; }

            public int? UserCreatedCallOrder { get; private set; }
            public int? UserRegisteredCallOrder { get; private set; }
            public int? SkillsAddedCallOrder { get; private set; }
            public int? SkillsRemovedCallOrder { get; private set; }


            public int? AfterHandlers1CallOrder { get; private set; }
            public int? AfterHandlers2CallOrder { get; private set; }

            public UserQueryModel TheModel => Model;

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

        interface IUserEvent : IAggregateRootEvent {}

        interface IUserCreatedEvent : IUserEvent, IAggregateRootCreatedEvent {}

        interface IUserRegistered : IUserCreatedEvent {}

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

        class UserDeleted : AggregateRootEvent, IUserDeleted {
            public UserDeleted(Guid userId):base(userId)
            {
            }
        }

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
