using System;
using System.Collections.Generic;
using System.Configuration;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Aggregates;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System.Configuration;
using Composable.System.Data.SqlClient;
using Composable.System.Threading;
using Composable.SystemExtensions.Threading;

namespace Composable.Messaging.Buses
{
    static class EndpointBuilderTypeMapperHelper
    {
        static string WithPostFix(this string guidTemplate, char postfix) => guidTemplate.Substring(0, guidTemplate.Length - 1) + postfix;

        static class Postfix
        {
            internal const char TypeItself = '1';
            internal const char Array = '2';
            internal const char List = '3';
            internal const char StringDictionary = '4';
        }

        public static ITypeMappingRegistar MapTypeAndStandardCollectionTypes<TType>(this ITypeMappingRegistar @this, string guidTemplate)
        {
            @this.Map<TType>(guidTemplate.WithPostFix(Postfix.TypeItself));

            @this.MapStandardCollectionTypes<TType>(guidTemplate);
            return @this;
        }

        public static ITypeMappingRegistar MapStandardCollectionTypes<TType>(this ITypeMappingRegistar @this, string guidTemplate)
        {
            @this.Map<TType[]>(guidTemplate.WithPostFix(Postfix.Array));
            @this.Map<List<TType>>(guidTemplate.WithPostFix(Postfix.List));
            @this.Map<Dictionary<string,TType>>(guidTemplate.WithPostFix(Postfix.StringDictionary));
            return @this;
        }
    }

    class EndpointBuilder : IEndpointBuilder
    {
        static readonly ISqlConnection MasterDbConnection = new AppConfigSqlConnectionProvider().GetConnectionProvider(parameterName: "MasterDB");

        readonly IDependencyInjectionContainer _container;
        readonly string _name;
        readonly TypeMapper _typeMapper;
        readonly EndpointId _endpointId;

        public IDependencyInjectionContainer Container => _container;
        public ITypeMappingRegistar TypeMapper => _typeMapper;
        public EndpointConfiguration Configuration { get; }

        public MessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }

        public IEndpoint Build()
        {
            SetupInternalTypeMap();
            return new Endpoint(_container.CreateServiceLocator(), _endpointId, _name);
        }


        void SetupInternalTypeMap()
        {
            EventStoreApi.MapTypes(TypeMapper);
            BusApi.MapTypes(TypeMapper);
        }

        public EndpointBuilder(IGlobalBusStateTracker globalStateTracker, IDependencyInjectionContainer container, string name, EndpointId endpointId)
        {
            _container = container;
            _name = name;
            _endpointId = endpointId;
            _typeMapper = new TypeMapper();
            var registry = new MessageHandlerRegistry(_typeMapper);

            Configuration = new EndpointConfiguration(name);

            RegisterHandlers = new MessageHandlerRegistrarWithDependencyInjectionSupport(registry, new Lazy<IServiceLocator>(() => _container.CreateServiceLocator()));

            DefaultWiring(globalStateTracker, _container, endpointId, Configuration, _typeMapper, registry);
        }

        internal static void DefaultWiring(IGlobalBusStateTracker globalStateTracker, IDependencyInjectionContainer container, EndpointId endpointId, EndpointConfiguration configuration, TypeMapper typeMapper, MessageHandlerRegistry registry)
        {
            var sqlServerConnection = container.RunMode.IsTesting
                                          ? new LazySqlServerConnection(new Lazy<string>(() => container.CreateServiceLocator().Resolve<ISqlConnectionProvider>().GetConnectionProvider(configuration.ConnectionStringName).ConnectionString))
                                          : new SqlServerConnection(ConfigurationManager.ConnectionStrings[configuration.ConnectionStringName].ConnectionString);

            container.Register(
                Component.For<ITaskRunner>().ImplementedBy<TaskRunner>().LifestyleSingleton(),
                Component.For<EndpointId>().UsingFactoryMethod(() => endpointId).LifestyleSingleton(),
                Component.For<EndpointConfiguration>()
                         .UsingFactoryMethod(() => configuration)
                         .LifestyleSingleton(),
                Component.For<ITypeMappingRegistar, ITypeMapper, TypeMapper>()
                         .UsingFactoryMethod(() => typeMapper)
                         .LifestyleSingleton()
                         .DelegateToParentServiceLocatorWhenCloning(),
                Component.For<IAggregateTypeValidator>()
                         .ImplementedBy<AggregateTypeValidator>()
                         .LifestyleSingleton(),
                Component.For<IInterprocessTransport>()
                         .UsingFactoryMethod((IUtcTimeTimeSource timeSource, ISqlConnectionProvider connectionProvider, EndpointId id, ITaskRunner taskRunner, IRemotableMessageSerializer serializer) =>
                                                 new InterprocessTransport(globalStateTracker, timeSource, sqlServerConnection, typeMapper, id, taskRunner, serializer))
                         .LifestyleSingleton(),
                Component.For<ISingleContextUseGuard>()
                         .ImplementedBy<SingleThreadUseGuard>()
                         .LifestyleScoped(),
                Component.For<IGlobalBusStateTracker>()
                         .UsingFactoryMethod(() => globalStateTracker)
                         .LifestyleSingleton(),
                Component.For<IMessageHandlerRegistry, IMessageHandlerRegistrar, MessageHandlerRegistry>()
                         .UsingFactoryMethod(() => registry)
                         .LifestyleSingleton(),
                Component.For<IEventStoreSerializer>()
                         .ImplementedBy<EventStoreSerializer>()
                         .LifestyleSingleton(),
                Component.For<IDocumentDbSerializer>()
                         .ImplementedBy<DocumentDbSerializer>()
                         .LifestyleSingleton(),
                Component.For<IRemotableMessageSerializer>()
                         .ImplementedBy<RemotableMessageSerializer>()
                         .LifestyleSingleton(),
                Component.For<IInbox>()
                         .UsingFactoryMethod((IServiceLocator serviceLocator, IGlobalBusStateTracker stateTracker, EndpointConfiguration endpointConfiguration, ITaskRunner taskRunner, IRemotableMessageSerializer serializer) =>
                                                 new Inbox(serviceLocator,stateTracker, registry, endpointConfiguration, sqlServerConnection, typeMapper, taskRunner, serializer))
                         .LifestyleSingleton(),
                Component.For<CommandScheduler>()
                         .UsingFactoryMethod((IInterprocessTransport transport, IUtcTimeTimeSource timeSource) => new CommandScheduler(transport, timeSource))
                         .LifestyleSingleton(),
                Component.For<IServiceBusControl>()
                         .ImplementedBy<ServiceBusControl>()
                         .LifestyleSingleton(),
                Component.For<IServiceBusSession, IRemoteApiNavigatorSession, ILocalApiNavigatorSession>()
                         .ImplementedBy<ApiNavigatorSession>()
                         .LifestyleScoped(),
                Component.For<IEventstoreEventPublisher>()
                         .ImplementedBy<EventstoreEventPublisher>()
                         .LifestyleScoped(),
                Component.For<ISqlConnectionProvider>()
                         .UsingFactoryMethod(() => new SqlServerDatabasePoolSqlConnectionProvider(MasterDbConnection.ConnectionString))
                         .LifestyleSingleton()
                         .DelegateToParentServiceLocatorWhenCloning());

            if(container.RunMode == RunMode.Production)
            {
                container.Register(Component.For<IUtcTimeTimeSource>()
                                            .UsingFactoryMethod(() => new DateTimeNowTimeSource())
                                            .LifestyleSingleton()
                                            .DelegateToParentServiceLocatorWhenCloning());
            } else
            {
                container.Register(Component.For<IUtcTimeTimeSource, TestingTimeSource>()
                                            .UsingFactoryMethod(() => TestingTimeSource.FollowingSystemClock)
                                            .LifestyleSingleton()
                                            .DelegateToParentServiceLocatorWhenCloning());
            }
        }
    }
}
