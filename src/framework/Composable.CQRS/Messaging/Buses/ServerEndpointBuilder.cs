using System;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses.Implementation;
using Composable.Messaging.Hypermedia;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Aggregates;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System;
using Composable.System.Configuration;
using Composable.System.Threading;

// ReSharper disable ImplicitlyCapturedClosure it is very much intentional :)

namespace Composable.Messaging.Buses
{
    class ServerEndpointBuilder : IEndpointBuilder
    {
        readonly IDependencyInjectionContainer _container;
        readonly TypeMapper _typeMapper;
        bool _builtSuccessfully;

        public IDependencyInjectionContainer Container => _container;
        public ITypeMappingRegistar TypeMapper => _typeMapper;
        readonly IGlobalBusStateTracker _globalStateTracker;
        readonly MessageHandlerRegistry _registry;
        readonly IEndpointHost _host;
        public EndpointConfiguration Configuration { get; }

        public MessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }

        public IEndpoint Build()
        {
            SetupContainer();
            SetupInternalTypeMap();
            MessageTypes.Internal.RegisterHandlers(RegisterHandlers);
            var serviceLocator = _container.CreateServiceLocator();
            var endpoint = new Endpoint(serviceLocator,
                                        serviceLocator.Resolve<IGlobalBusStateTracker>(),
                                        serviceLocator.Resolve<IOutbox>(),
                                        serviceLocator.Resolve<IEndpointRegistry>(),
                                        serviceLocator.Resolve<IOutbox>(),
                                        Configuration);
            _builtSuccessfully = true;
            return endpoint;
        }

        void SetupInternalTypeMap()
        {
            EventStoreApi.MapTypes(TypeMapper);
            MessageTypes.MapTypes(TypeMapper);
        }

        public ServerEndpointBuilder(IEndpointHost host, IGlobalBusStateTracker globalStateTracker, IDependencyInjectionContainer container, EndpointConfiguration configuration)
        {
            _host = host;
            _container = container;
            _globalStateTracker = globalStateTracker;


            Configuration = configuration;

            _typeMapper = new TypeMapper();

            _registry = new MessageHandlerRegistry(_typeMapper);
            RegisterHandlers = new MessageHandlerRegistrarWithDependencyInjectionSupport(_registry, new OptimizedLazy<IServiceLocator>(() => _container.CreateServiceLocator()));

        }

        void SetupContainer()
        {
            //todo: Find cleaner way of doing this.
            if(_host is IEndpointRegistry endpointRegistry)
            {
                _container.Register(Singleton.For<IEndpointRegistry>().Instance(endpointRegistry));
            } else
            {
                _container.Register(Singleton.For<IEndpointRegistry>().CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => new AppConfigEndpointRegistry(configurationParameterProvider)));
            }

            if(!_container.HasComponent<IConfigurationParameterProvider>())
            {
                _container.Register(Singleton.For<IConfigurationParameterProvider>().CreatedBy(() => new AppSettingsJsonConfigurationParameterProvider()));
            }

            _container.Register(
                Singleton.For<ITypeMappingRegistar, ITypeMapper, TypeMapper>().CreatedBy(() => _typeMapper).DelegateToParentServiceLocatorWhenCloning(),
                Singleton.For<ITaskRunner>().CreatedBy(() => new TaskRunner()),
                Singleton.For<EndpointId>().CreatedBy(() => Configuration.Id),
                Singleton.For<EndpointConfiguration>().CreatedBy(() => Configuration),


                Singleton.For<IOutbox>().CreatedBy((IUtcTimeTimeSource timeSource, RealEndpointConfiguration configuration, ITaskRunner taskRunner, IRemotableMessageSerializer serializer, Outbox.IMessageStorage messageStorage) 
                                                       => new Outbox(_globalStateTracker, timeSource, messageStorage, _typeMapper, configuration, taskRunner, serializer)),
                Singleton.For<IGlobalBusStateTracker>().CreatedBy(() => _globalStateTracker),
                Singleton.For<IMessageHandlerRegistry, IMessageHandlerRegistrar, MessageHandlerRegistry>().CreatedBy(() => _registry),
                Singleton.For<IEventStoreSerializer>().CreatedBy(() => new EventStoreSerializer(_typeMapper)),
                Singleton.For<IDocumentDbSerializer>().CreatedBy(() => new DocumentDbSerializer(_typeMapper)),
                Singleton.For<IRemotableMessageSerializer>().CreatedBy(() => new RemotableMessageSerializer(_typeMapper)),
                Singleton.For<IAggregateTypeValidator>().CreatedBy(() => new AggregateTypeValidator(_typeMapper)),
                Singleton.For<IEventStoreEventPublisher>().CreatedBy((IOutbox outbox, IMessageHandlerRegistry messageHandlerRegistry) => new ServiceBusEventStoreEventPublisher(outbox, messageHandlerRegistry)),
                Scoped.For<IRemoteHypermediaNavigator>().CreatedBy((IOutbox outbox) => new RemoteApiBrowserSession(outbox)),
                Singleton.For<RealEndpointConfiguration>().CreatedBy((EndpointConfiguration conf, IConfigurationParameterProvider configurationParameterProvider) => new RealEndpointConfiguration(conf, configurationParameterProvider)));

            if(Configuration.HasMessageHandlers)
            {
                _container.Register(
                    Singleton.For<IInbox>().CreatedBy((IServiceLocator serviceLocator, RealEndpointConfiguration endpointConfiguration, ITaskRunner taskRunner, IRemotableMessageSerializer serializer, Inbox.IMessageStorage messageStorage) => new Inbox(serviceLocator, _globalStateTracker, _registry, endpointConfiguration, messageStorage, _typeMapper, taskRunner, serializer)),
                    Singleton.For<CommandScheduler>().CreatedBy((IOutbox transport, IUtcTimeTimeSource timeSource) => new CommandScheduler(transport, timeSource)),
                    Scoped.For<IServiceBusSession, ILocalHypermediaNavigator>().CreatedBy((IOutbox outbox, CommandScheduler commandScheduler, IMessageHandlerRegistry messageHandlerRegistry, IRemoteHypermediaNavigator remoteNavigator) => new ApiNavigatorSession(outbox, commandScheduler, messageHandlerRegistry, remoteNavigator))
                );
            }

            if(_container.RunMode == RunMode.Production)
            {
                _container.Register(Singleton.For<IUtcTimeTimeSource>().CreatedBy(() => new DateTimeNowTimeSource()).DelegateToParentServiceLocatorWhenCloning());
            } else
            {
                _container.Register(Singleton.For<IUtcTimeTimeSource, TestingTimeSource>().CreatedBy(() => TestingTimeSource.FollowingSystemClock).DelegateToParentServiceLocatorWhenCloning());
            }
        }

        bool _disposed;
        public void Dispose()
        {
            if(!_disposed)
            {
                _disposed = true;
                if(!_builtSuccessfully)
                {
                    _container.Dispose();
                }
            }
        }
    }
}
