using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;
using Composable.GenericAbstractions.Time;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Aggregates;
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
                                 new EndpointId(Guid.Parse("A4A2BA96-8D82-47AC-8A1B-38476C7B5D5D")),
                                 builder =>
                                 {
                                     builder.Container.RegisterSqlServerEventStore<IUserEventStoreUpdater, IUserEventStoreReader>(builder.Configuration.ConnectionStringName);

                                     builder.RegisterHandlers
                                            .ForEvent((UserEvent.Implementation.UserRegisteredEvent myEvent) => {})
                                            .ForQuery((GetUserQuery query, IUserEventStoreReader eventReader) => new UserResource(eventReader.GetHistory(query.UserId)))
                                            .ForCommandWithResult((UserRegistrarCommand.RegisterUserCommand command, IUserEventStoreUpdater store) =>
                                            {
                                                store.Save(UserAggregate.Register(command));
                                                return new RegisterUserResult(command.UserId);
                                            });

                                     builder.TypeMapper
                                            .Map<GetUserQuery>("f9163a11-c6b6-4d2f-88e4-fd476b95dc07")
                                            .Map<UserRegistrarCommand.RegisterUserCommand>("99ab8e1f-ce88-4070-becc-7967d65de172")
                                            .Map<UserAggregate>("4281ba68-a3ce-4a9a-82fc-be71d6155fbc")
                                            .Map<UserRegistrarAggregate>("732f0613-ee94-4d03-a479-9e5b69dc0e69")
                                            .Map<UserRegistrarEvent.Implementation.Created>("0e97953f-57f5-4252-8dec-a31c9a387dac")
                                            .Map<UserRegistrarEvent.Implementation.Root>("1849d406-9af4-481f-a475-395e9112ac4a")
                                            .Map<UserRegistrarEvent.IRoot>("20033612-88c5-422b-9632-d4d3cbcaff45")
                                            .Map<UserEvent.Implementation.Root>("05f0f69f-c29a-49c0-8cea-62286f5a1816")
                                            .Map<UserEvent.Implementation.UserRegisteredEvent>("5eac2b7a-014a-4783-9b19-4f0f975028f4")
                                            .Map<UserEvent.IRoot>("ff9f3cae-7377-4865-a623-f11436dad926")
                                            .Map<UserEvent.UserRegistered>("1b5e0128-ab76-4026-a6d7-4f2ffa4d82cd");
                                 }));

            _userDomainServiceLocator = _userManagementDomainEndpoint.ServiceLocator;

            _userDomainServiceLocator.ExecuteTransactionInIsolatedScope(() => _userDomainServiceLocator.Resolve<IUserEventStoreUpdater>().Save(UserRegistrarAggregate.Create()));


        }

        [Fact] void Can_register_user_and_fetch_user_resource()
        {
            var registrationResult = _userDomainServiceLocator.ExecuteTransactionInIsolatedScope(() =>  UserRegistrarAggregate.RegisterUser(_userDomainServiceLocator.Resolve<IServiceBusSession>()));

            var user = _host.ClientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() =>_host.ClientBusSession.GetRemote(registrationResult.UserLink));

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
            public interface IRoot : IAggregateEvent {}

            public interface UserRegistered : IRoot, IAggregateCreatedEvent {}

            public static class Implementation
            {
                public class Root : AggregateEvent, IRoot
                {
                    protected Root() {}
                    protected Root(Guid aggregateId) : base(aggregateId) {}
                }

                public class UserRegisteredEvent : Root, UserRegistered
                {
                    public UserRegisteredEvent(Guid userId) : base(userId) {}
                }
            }
        }

        public static class UserRegistrarCommand
        {
            public interface IRoot : MessagingApi.Remote.ExactlyOnce.IExactlyOnceCommand {}

            public class Root : ExactlyOnceCommand, IRoot {}
            public class Root<TResult> : ExactlyOnceCommand<TResult>, IRoot where TResult : MessagingApi.IMessage {}

            public class RegisterUserCommand : Root<RegisterUserResult>
            {
                public Guid UserId { get; private set; } = Guid.NewGuid();
            }
        }

        public static class UserRegistrarEvent
        {
            public interface IRoot : IAggregateEvent {}
            public static class Implementation
            {
                public class Root : AggregateEvent, IRoot
                {
                    protected Root() {}
                    protected Root(Guid aggregateId) : base(aggregateId) {}
                }

                public class Created : Root, IAggregateCreatedEvent
                {
                    public Created() : base(UserRegistrarAggregate.SingleId) {}
                }
            }
        }

        public class UserRegistrarAggregate : Aggregate<UserRegistrarAggregate, UserRegistrarEvent.Implementation.Root, UserRegistrarEvent.IRoot>
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

            internal static RegisterUserResult RegisterUser(IServiceBusSession bus) => new UserRegistrarCommand.RegisterUserCommand().PostLocalOn(bus);
        }

        public class UserAggregate : Aggregate<UserAggregate, UserEvent.Implementation.Root, UserEvent.IRoot>
        {
            UserAggregate() : base(DateTimeNowTimeSource.Instance)
                => RegisterEventAppliers()
                    .IgnoreUnhandled<UserEvent.IRoot>();

            internal static UserAggregate Register(UserRegistrarCommand.RegisterUserCommand command)
            {
                var registered = new UserAggregate();
                registered.Publish(new UserEvent.Implementation.UserRegisteredEvent(command.UserId));
                return registered;
            }
        }

        public class GetUserQuery : RemoteQuery<UserResource>
        {
            public Guid UserId { get; private set; }
            public GetUserQuery(Guid userId) => UserId = userId;
        }

        public class UserResource : QueryResult
        {
            public IEnumerable<IAggregateEvent> History { get; }
            public UserResource(IEnumerable<IAggregateEvent> history) { History = history; }
        }

        public class RegisterUserResult : ExactlyOnceMessage
        {
            public GetUserQuery UserLink { get; private set; }
            public RegisterUserResult(Guid userId) => UserLink = new GetUserQuery(userId);
        }
    }
}
