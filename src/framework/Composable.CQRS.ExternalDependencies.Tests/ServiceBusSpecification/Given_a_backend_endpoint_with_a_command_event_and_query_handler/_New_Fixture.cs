using System;
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

namespace Composable.CQRS.Tests.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class New_Fixture : IDisposable
    {
        internal readonly ITestingEndpointHost Host;

        protected readonly TestingTaskRunner TaskRunner = TestingTaskRunner.WithTimeout(1.Seconds());
        IEndpoint _userManagementDomainEndpoint;
        IServiceLocator _userDomainServiceLocator;

        public New_Fixture()
        {
            Host = EndpointHost.Testing.BuildHost(
                buildHost => _userManagementDomainEndpoint = buildHost.RegisterAndStartEndpoint(
                                 "UserManagement.Domain",
                                 builder =>
                                 {
                                     builder.Container.RegisterSqlServerEventStore<IUserEventStoreUpdater, IUserEventStoreReader>("Someconnectionname");

                                     builder.RegisterHandlers
                                            .ForEvent((UserEvent.Implementation.UserRegisteredEvent myEvent) => {})
                                            .ForQuery((GetUserQuery query) => new UserResource())
                                            .ForCommand((UserRegistrarCommand.RegisterUserCommand command, IUserEventStoreUpdater store) =>
                                                            store.Save(UserAggregate.Register(command)));
                                 }));

            _userDomainServiceLocator = _userManagementDomainEndpoint.ServiceLocator;
            _userDomainServiceLocator.ExecuteTransactionInIsolatedScope(() => _userDomainServiceLocator.Resolve<IUserEventStoreUpdater>().Save(UserRegistrarAggregate.Create()));
        }

        [Fact] void FACT() => _userDomainServiceLocator.ExecuteTransactionInIsolatedScope(
            () => UserRegistrarAggregate.RegisterUser(_userDomainServiceLocator.Resolve<IServiceBus>()));

        public virtual void Dispose()
        {
            TaskRunner.Dispose();
            Host.Dispose();
        }

        public interface IUserEventStoreUpdater : IEventStoreUpdater {}

        public interface IUserEventStoreReader : IEventStoreReader {}

        public static class UserEvent
        {
            public interface IRoot : IAggregateRootEvent {}

            public interface UserRegistered : IRoot, IAggregateRootCreatedEvent {}

            public static class Implementation
            {
                public class Root : AggregateRootEvent, IRoot
                {
                    protected Root() {}
                    protected Root(Guid aggregateRootId) : base(aggregateRootId) {}
                }

                public class UserRegisteredEvent : Root, UserEvent.UserRegistered
                {
                    public UserRegisteredEvent() : base(Guid.NewGuid()) {}
                }
            }
        }

        public static class UserRegistrarCommand
        {
            public interface IRoot : ICommand {}

            public class Root : Command, IRoot {}
            public class Root<TResult> : Command<TResult>, IRoot where TResult : IMessage {}

            public class RegisterUserCommand : Root<RegisterUserResult> {}
        }

        public static class UserRegistrarEvent
        {
            public interface IRoot : IAggregateRootEvent {}
            public static class Implementation
            {
                public class Root : AggregateRootEvent, IRoot
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
                registrar.RaiseEvent(new UserRegistrarEvent.Implementation.Created());
                return registrar;
            }

            UserRegistrarAggregate() : base(DateTimeNowTimeSource.Instance)
                => RegisterEventAppliers()
                    .IgnoreUnhandled<UserRegistrarEvent.IRoot>();

            internal static void RegisterUser(IServiceBus bus) { bus.Send(new UserRegistrarCommand.RegisterUserCommand()); }
        }

        public class UserAggregate : AggregateRoot<UserAggregate, UserEvent.Implementation.Root, UserEvent.IRoot>
        {
            UserAggregate() : base(DateTimeNowTimeSource.Instance)
                => RegisterEventAppliers()
                    .IgnoreUnhandled<UserEvent.IRoot>();

            internal static IEventStored Register(UserRegistrarCommand.RegisterUserCommand command)
            {
                var registered = new UserAggregate();
                registered.RaiseEvent(new UserEvent.Implementation.UserRegisteredEvent());
                return registered;
            }
        }

        class GetUserQuery : Query<UserResource> {}
        class UserResource : QueryResult {}
        public class RegisterUserResult : Message {}
    }
}
