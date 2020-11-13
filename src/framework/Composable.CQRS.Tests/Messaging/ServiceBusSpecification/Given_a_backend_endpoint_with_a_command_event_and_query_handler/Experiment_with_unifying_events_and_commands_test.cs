using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Messaging.Hypermedia;
using Composable.Persistence.Common.DependencyInjection;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Aggregates;
using Composable.SystemCE;
using Composable.Testing;
using Composable.Testing.Threading;
using FluentAssertions;
using NUnit.Framework;
// ReSharper disable MemberCanBeInternal for testing
// ReSharper disable InconsistentNaming for testing
#pragma warning disable CA1724 // Type names should not match namespaces

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Experiment_with_unifying_events_and_commands_test : DuplicateByPluggableComponentTest
    {
        ITestingEndpointHost _host;

        readonly TestingTaskRunner _taskRunner = TestingTaskRunner.WithTimeout(1.Seconds());
        IServiceLocator _userDomainServiceLocator;
        IEndpoint _clientEndpoint;

        IRemoteHypermediaNavigator RemoteNavigator => _clientEndpoint.ServiceLocator.Resolve<IRemoteHypermediaNavigator>();

        [SetUp] public async Task Setup()
        {
            _host = TestingEndpointHost.Create(DependencyInjectionContainer.Create);

            var userManagementDomainEndpoint = _host.RegisterEndpoint(
                "UserManagement.Domain",
                new EndpointId(Guid.Parse("A4A2BA96-8D82-47AC-8A1B-38476C7B5D5D")),
                builder =>
                {
                    builder.RegisterCurrentTestsConfiguredPersistenceLayer();
                    builder.Container.RegisterEventStore(builder.Configuration.ConnectionStringName);

                    builder.RegisterHandlers
                           .ForEvent((UserEvent.IUserRegistered myEvent) => {})
                           .ForQuery((GetUserQuery query, IEventStoreReader eventReader) => new UserResource(eventReader.GetHistory(query.UserId)))
                           .ForCommandWithResult((UserRegistrarCommand.RegisterUserCommand command, IEventStoreUpdater store) =>
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
                           .Map<UserEvent.IUserRegistered>("1b5e0128-ab76-4026-a6d7-4f2ffa4d82cd")
                           .Map<RegisterUserResult>("940adbc5-ef68-436a-90c2-ac4f000ec377")
                           .Map<UserResource>("9f621299-22d9-4888-81f1-0e9ebc09625c");
                });

            _clientEndpoint = _host.RegisterClientEndpointForRegisteredEndpoints();

            await _host.StartAsync();

            _userDomainServiceLocator = userManagementDomainEndpoint.ServiceLocator;

            _userDomainServiceLocator.ExecuteTransactionInIsolatedScope(() => _userDomainServiceLocator.Resolve<IEventStoreUpdater>().Save(UserRegistrarAggregate.Create()));
        }

        [Test] public void Can_register_user_and_fetch_user_resource()
        {
            var registrationResult = _userDomainServiceLocator.ExecuteInIsolatedScope(() =>  UserRegistrarAggregate.RegisterUser(_userDomainServiceLocator.Resolve<IRemoteHypermediaNavigator>()));

            var user = _clientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => RemoteNavigator.Get(registrationResult.UserLink));

            user.Should().NotBe(null);
            user.History.Count().Should().Be(1);
        }


        [TearDown]public void Teardown()
        {
            _taskRunner.Dispose();
            _host.Dispose();
        }

        public static class UserEvent
        {
            public interface IRoot : IAggregateEvent {}

            public interface IUserRegistered : IRoot, IAggregateCreatedEvent {}

            public static class Implementation
            {
                public class Root : AggregateEvent, IRoot
                {
                    protected Root() {}
                    protected Root(Guid aggregateId) : base(aggregateId) {}
                }

                public class UserRegisteredEvent : Root, IUserRegistered
                {
                    public UserRegisteredEvent(Guid userId) : base(userId) {}
                }
            }
        }

        public static class UserRegistrarCommand
        {
            public class RegisterUserCommand : MessageTypes.Remotable.AtMostOnce.AtMostOnceCommand<RegisterUserResult>
            {
                public Guid UserId { get; private set; } = Guid.NewGuid();

                RegisterUserCommand() : base(DeduplicationIdHandling.Reuse) {}

                internal static RegisterUserCommand Create() => new RegisterUserCommand { MessageId =  Guid.NewGuid()};
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

            internal static RegisterUserResult RegisterUser(IRemoteHypermediaNavigator navigator) => UserRegistrarCommand.RegisterUserCommand.Create().PostOn(navigator);
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

        public class GetUserQuery : MessageTypes.Remotable.NonTransactional.Queries.Query<UserResource>
        {
            public Guid UserId { get; private set; }
            public GetUserQuery(Guid userId) => UserId = userId;
        }

        public class UserResource
        {
            public IEnumerable<IAggregateEvent> History { get; }
            public UserResource(IEnumerable<IAggregateEvent> history) { History = history; }
        }

        public class RegisterUserResult
        {
            public GetUserQuery UserLink { get; private set; }
            public RegisterUserResult(Guid userId) => UserLink = new GetUserQuery(userId);
        }

        public Experiment_with_unifying_events_and_commands_test(string _) : base(_) {}
    }
}
