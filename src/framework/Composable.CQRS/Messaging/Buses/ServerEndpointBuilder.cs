using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses.Implementation;
using Composable.Messaging.Hypermedia;
using Composable.Persistence.EventStore;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.SystemCE;
using Composable.SystemCE.ConfigurationCE;
using Composable.SystemCE.ThreadingCE;

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
                                        serviceLocator.Resolve<ITransport>(),
                                        serviceLocator.Resolve<IEndpointRegistry>(),
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

            if(_container.RunMode == RunMode.Production)
            {
                _container.Register(Singleton.For<IUtcTimeTimeSource>().CreatedBy(() => new DateTimeNowTimeSource()).DelegateToParentServiceLocatorWhenCloning());
            } else
            {
                _container.Register(Singleton.For<IUtcTimeTimeSource, TestingTimeSource>().CreatedBy(() => TestingTimeSource.FollowingSystemClock).DelegateToParentServiceLocatorWhenCloning());
            }

            _container.Register(Singleton.For<IConfigurationParameterProvider>().CreatedBy(() => new AppSettingsJsonConfigurationParameterProvider()));

            _container.Register(
                Singleton.For<ITypeMappingRegistar, ITypeMapper, TypeMapper>().CreatedBy(() => _typeMapper).DelegateToParentServiceLocatorWhenCloning(),


                Singleton.For<IGlobalBusStateTracker>().CreatedBy(() => _globalStateTracker),


                Singleton.For<IRemotableMessageSerializer>().CreatedBy((ITypeMapper typeMapper) => new RemotableMessageSerializer(typeMapper)),

                Singleton.For<ITransport>().CreatedBy((IGlobalBusStateTracker globalBusStateTracker, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
                                                          => new Transport(globalBusStateTracker, typeMapper, serializer)),

                Scoped.For<IRemoteHypermediaNavigator>().CreatedBy((ITransport transport) => new RemoteHypermediaNavigator(transport))

            );

            if(!Configuration.IsPureClientEndpoint)
            {
                _container.Register(
                    Singleton.For<EndpointId>().CreatedBy(() => Configuration.Id),
                    Singleton.For<EndpointConfiguration>().CreatedBy(() => Configuration),

                    Singleton.For<IMessageHandlerRegistry, IMessageHandlerRegistrar, MessageHandlerRegistry>().CreatedBy(() => _registry),

                    Singleton.For<ITaskRunner>().CreatedBy(() => new TaskRunner()),

                    Singleton.For<RealEndpointConfiguration>().CreatedBy((EndpointConfiguration conf, IConfigurationParameterProvider configurationParameterProvider)
                                                                             => new RealEndpointConfiguration(conf, configurationParameterProvider)),

                    Singleton.For<Outbox.IMessageStorage>()
                             .CreatedBy((IServiceBusPersistenceLayer.IOutboxPersistenceLayer persistenceLayer, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
                                            => new Outbox.MessageStorage(persistenceLayer, typeMapper, serializer)),
                    Singleton.For<IOutbox>().CreatedBy((RealEndpointConfiguration configuration, ITransport transport, Outbox.IMessageStorage messageStorage)
                                                           => new Outbox(transport, messageStorage, configuration)),

                    Singleton.For<IEventStoreEventPublisher>().CreatedBy((IOutbox outbox, IMessageHandlerRegistry messageHandlerRegistry)
                                                                             => new ServiceBusEventStoreEventPublisher(outbox, messageHandlerRegistry)),

                    Singleton.For<Inbox.IMessageStorage>().CreatedBy((IServiceBusPersistenceLayer.IInboxPersistenceLayer persistenceLayer) => new InboxMessageStorage(persistenceLayer)),
                    Singleton.For<IInbox>().CreatedBy((IServiceLocator serviceLocator, RealEndpointConfiguration endpointConfiguration, ITaskRunner taskRunner, IRemotableMessageSerializer serializer, Inbox.IMessageStorage messageStorage)
                                                          => new Inbox(serviceLocator, _globalStateTracker, _registry, endpointConfiguration, messageStorage, _typeMapper, taskRunner, serializer)),
                    Singleton.For<CommandScheduler>().CreatedBy((IOutbox transport, IUtcTimeTimeSource timeSource, ITaskRunner taskRunner) => new CommandScheduler(transport, timeSource, taskRunner)),
                    Scoped.For<IServiceBusSession>().CreatedBy((IOutbox outbox, CommandScheduler commandScheduler) => new ServiceBusSession(outbox, commandScheduler)),
                    Scoped.For<ILocalHypermediaNavigator>().CreatedBy((IMessageHandlerRegistry messageHandlerRegistry) => new LocalHypermediaNavigator(messageHandlerRegistry))
                );
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
