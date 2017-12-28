using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;
using Composable.GenericAbstractions.Time;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Messaging.Commands;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.AggregateRoots;
using Composable.Testing.Threading;
using FluentAssertions;
using Xunit;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    using Composable.System;

    public class Experiment_with_unifying_events_and_commands_test : IDisposable
    {
        readonly ITestingEndpointHost _host;

        readonly TestingTaskRunner _taskRunner = TestingTaskRunner.WithTimeout(1.Seconds());
        IEndpoint _userManagementDomainEndpoint;
        readonly IServiceLocator _userDomainServiceLocator;

        public Experiment_with_unifying_events_and_commands_test()
        {
            _host = EndpointHost.Testing.BuildHost(
                DependencyInjectionContainer.Create,
                buildHost => _userManagementDomainEndpoint = buildHost.RegisterAndStartEndpoint(
                                 "UserManagement.Domain",
                                 builder =>
                                 {
                                     builder.Container.RegisterSqlServerEventStore<IUserEventStoreUpdater, IUserEventStoreReader>("SomeConnectionName");

                                     builder.RegisterHandlers
                                            .ForEvent((UserEvent.Implementation.UserRegisteredEvent myEvent) => {})
                                            .ForQuery((GetUserQuery query, IUserEventStoreReader eventReader) => new UserResource(eventReader.GetHistory(query.UserId)))
                                            .ForCommandWithResult((UserRegistrarCommand.RegisterUserCommand command, IUserEventStoreUpdater store) =>
                                            {
                                                store.Save(UserAggregate.Register(command));
                                                return new RegisterUserResult(command.UserId);
                                            });
                                 }));

            _userDomainServiceLocator = _userManagementDomainEndpoint.ServiceLocator;
            _userDomainServiceLocator.ExecuteTransactionInIsolatedScope(() => _userDomainServiceLocator.Resolve<IUserEventStoreUpdater>().Save(UserRegistrarAggregate.Create()));
        }

        [Fact] async Task Can_register_user_and_fetch_user_resource()
        {
            var registrationResult = await _userDomainServiceLocator.ExecuteTransactionInIsolatedScope(
                                         () => UserRegistrarAggregate.RegisterUser(_userDomainServiceLocator.Resolve<IServiceBus>()));

            var user = _host.ClientBus.Query(registrationResult.UserLink);

            user.Should().NotBe(null);
            user.History.Count().Should().Be(1);
        }


        public void Dispose()
        {
            _taskRunner.Dispose();
            _host.Dispose();
        }

        public interface IUserEventStoreUpdater : IEventStoreUpdater {}

        public interface IUserEventStoreReader : IEventStoreReader {}

        public static class UserEvent
        {
            public interface IRoot : IDomainEvent {}

            public interface UserRegistered : IRoot, IAggregateRootCreatedEvent {}

            public static class Implementation
            {
                public class Root : DomainEvent, IRoot
                {
                    protected Root() {}
                    protected Root(Guid aggregateRootId) : base(aggregateRootId) {}
                }

                public class UserRegisteredEvent : Root, UserEvent.UserRegistered
                {
                    public UserRegisteredEvent(Guid userId) : base(userId) {}
                }
            }
        }

        public static class UserRegistrarCommand
        {
            public interface IRoot : IDomainCommand {}

            public class Root : DomainCommand, IRoot {}
            public class Root<TResult> : DomainCommand<TResult>, IRoot where TResult : IMessage {}

            public class RegisterUserCommand : Root<RegisterUserResult>
            {
                public Guid UserId { get; private set; } = Guid.NewGuid();
            }
        }

        public static class UserRegistrarEvent
        {
            public interface IRoot : IDomainEvent {}
            public static class Implementation
            {
                public class Root : DomainEvent, IRoot
                {
                    protected Root() {}
                    protected Root(Guid aggregateRootId) : base(aggregateRootId) {}
                }

                public class Created : Root, IAggregateRootCreatedEvent
                {
                    public Created() : base(UserRegistrarAggregate.SingleId) {}
                }
            }
        }

        public class UserRegistrarAggregate : AggregateRoot<UserRegistrarAggregate, UserRegistrarEvent.Implementation.Root, UserRegistrarEvent.IRoot>
        {
            internal static Guid SingleId = Guid.Parse("5C400DD9-50FB-40C7-8A13-265005588AED");
            internal static UserRegistrarAggregate Create()
            {
                var registrar = new UserRegistrarAggregate();
                registrar.Publish(new UserRegistrarEvent.Implementation.Created());
                return registrar;
            }

            UserRegistrarAggregate() : base(DateTimeNowTimeSource.Instance)
                => RegisterEventAppliers()
                    .IgnoreUnhandled<UserRegistrarEvent.IRoot>();

            internal static async Task<RegisterUserResult> RegisterUser(IServiceBus bus) => await await bus.SendAsyncAsync(new UserRegistrarCommand.RegisterUserCommand());
        }

        public class UserAggregate : AggregateRoot<UserAggregate, UserEvent.Implementation.Root, UserEvent.IRoot>
        {
            UserAggregate() : base(DateTimeNowTimeSource.Instance)
                => RegisterEventAppliers()
                    .IgnoreUnhandled<UserEvent.IRoot>();

            internal static IEventStored Register(UserRegistrarCommand.RegisterUserCommand command)
            {
                var registered = new UserAggregate();
                registered.Publish(new UserEvent.Implementation.UserRegisteredEvent(command.UserId));
                return registered;
            }
        }

        public class GetUserQuery : Query<UserResource>
        {
            public Guid UserId { get; private set; }
            public GetUserQuery(Guid userId) => UserId = userId;
        }

        public class UserResource : QueryResult
        {
            public IEnumerable<IDomainEvent> History { get; }
            public UserResource(IEnumerable<IDomainEvent> history) { History = history; }
        }

        public class RegisterUserResult : Message
        {
            public GetUserQuery UserLink { get; private set; }
            public RegisterUserResult(Guid userId) => UserLink = new GetUserQuery(userId);
        }
    }
}
