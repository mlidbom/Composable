using System;
using System.Collections.Generic;
using System.Linq;
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
                                 }));

            _userDomainServiceLocator = _userManagementDomainEndpoint.ServiceLocator;
            _userDomainServiceLocator.ExecuteTransactionInIsolatedScope(() => _userDomainServiceLocator.Resolve<IUserEventStoreUpdater>().Save(UserRegistrarAggregate.Create()));
        }

        [Fact] void Can_register_user_and_fetch_user_resource()
        {
            var registrationResult = UserRegistrarAggregate.RegisterUser(_userDomainServiceLocator.Resolve<IServiceBus>());

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
            [TypeId("176EE1DD-8D56-4879-8230-46FCAF30E523")]public interface IRoot : IAggregateRootEvent {}

            [TypeId("86B52908-5201-45B5-B504-23776CB58480")]public interface UserRegistered : IRoot, IAggregateRootCreatedEvent {}

            public static class Implementation
            {
                [TypeId("F53F2EB7-F856-4743-B90D-6AD96C95883D")]public class Root : AggregateRootEvent, IRoot
                {
                    protected Root() {}
                    protected Root(Guid aggregateRootId) : base(aggregateRootId) {}
                }

                [TypeId("3210D879-0D81-4DAB-9254-CA85B9D70F69")]public class UserRegisteredEvent : Root, UserEvent.UserRegistered
                {
                    public UserRegisteredEvent(Guid userId) : base(userId) {}
                }
            }
        }

        public static class UserRegistrarCommand
        {
            public interface IRoot : ITransactionalExactlyOnceDeliveryCommand {}

            public class Root : TransactionalExactlyOnceDeliveryCommand, IRoot {}
            public class Root<TResult> : TransactionalExactlyOnceDeliveryCommand<TResult>, IRoot where TResult : IMessage {}

            [TypeId("ED0AFADB-AD2D-4212-833A-CB14266204ED")]public class RegisterUserCommand : Root<RegisterUserResult>
            {
                public Guid UserId { get; private set; } = Guid.NewGuid();
            }
        }

        public static class UserRegistrarEvent
        {
            [TypeId("B4C43E2D-5B17-4FE2-8E81-2135F6934807")]public interface IRoot : IAggregateRootEvent {}
            public static class Implementation
            {
                [TypeId("F4FE9D4A-082C-4B1E-A703-CD392E8D6946")]public class Root : AggregateRootEvent, IRoot
                {
                    protected Root() {}
                    protected Root(Guid aggregateRootId) : base(aggregateRootId) {}
                }

                [TypeId("0B8E14C7-56BA-4EC1-98D3-A213385ADB88")]public class Created : Root, IAggregateRootCreatedEvent
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

            internal static RegisterUserResult RegisterUser(IServiceBus bus) => bus.Send(new UserRegistrarCommand.RegisterUserCommand());
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

        [TypeId("ADFEAF4F-DD79-4BC1-9B34-82816CCAA752")]public class GetUserQuery : Query<UserResource>
        {
            public Guid UserId { get; private set; }
            public GetUserQuery(Guid userId) => UserId = userId;
        }

        public class UserResource : QueryResult
        {
            public IEnumerable<IAggregateRootEvent> History { get; }
            public UserResource(IEnumerable<IAggregateRootEvent> history) { History = history; }
        }

        public class RegisterUserResult : Message
        {
            public GetUserQuery UserLink { get; private set; }
            public RegisterUserResult(Guid userId) => UserLink = new GetUserQuery(userId);
        }
    }
}
