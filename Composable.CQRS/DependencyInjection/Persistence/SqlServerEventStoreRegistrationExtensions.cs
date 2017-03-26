using System;
using System.Collections.Generic;
using Castle.DynamicProxy;
using Castle.MicroKernel.Registration;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.MicrosoftSQLServer;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.System.Configuration;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;

namespace Composable.DependencyInjection.Persistence
{
    public abstract class SqlServerEventStoreRegistration
    {
        protected SqlServerEventStoreRegistration(string description, Type readerType)
        {
            ReaderType = readerType;
            StoreName = $"{description}.Store";
            SessionName = $"{description}.Session";
            SessionImplementationName = $"{description}.SessionImplementation";
        }

        internal string SessionImplementationName { get; }
        internal Type ReaderType { get; }
        internal string StoreName { get; }
        internal string SessionName { get; }
        internal ServiceOverride Store => Dependency.OnComponent(typeof(IEventStore), componentName: StoreName);

    }

    class SqlServerEventStoreRegistration<TSessionInterface, TReaderInterface> : SqlServerEventStoreRegistration
        where TSessionInterface : IEventStoreSession
        where TReaderInterface : IEventStoreReader
    {
        public SqlServerEventStoreRegistration() : base(typeof(TSessionInterface).FullName, readerType: typeof(TReaderInterface)) { }
    }

    public static class SqlServerEventStoreRegistrationExtensions
    {
        public static SqlServerEventStoreRegistration RegisterSqlServerEventStore<TSessionInterface, TReaderInterface>(this IDependencyInjectionContainer @this,
                                                                                                                       string connectionName,
                                                                                                                       IEnumerable<IEventMigration> migrations = null)
            where TSessionInterface : IEventStoreSession
            where TReaderInterface : IEventStoreReader =>
            @this.RegisterSqlServerEventStore<TSessionInterface, TReaderInterface>(
                                                                                   registration: new SqlServerEventStoreRegistration<TSessionInterface, TReaderInterface>(),
                                                                                   connectionName: connectionName,
                                                                                   migrations: migrations
                                                                                  );

        static SqlServerEventStoreRegistration RegisterSqlServerEventStore<TSessionInterface, TReaderInterface>
            (this IDependencyInjectionContainer @this,
             SqlServerEventStoreRegistration registration,
             string connectionName,
             IEnumerable<IEventMigration> migrations = null)
            where TSessionInterface : IEventStoreSession
            where TReaderInterface : IEventStoreReader
        {
            Contract.Argument(() => registration)
                        .NotNull();
            Contract.Argument(() => connectionName)
                        .NotNullEmptyOrWhiteSpace();

            var newContainer = @this;
            var serviceLocator = newContainer.CreateServiceLocator();

            GeneratedLowLevelInterfaceInspector.InspectInterfaces(Seq.OfTypes<TSessionInterface, TReaderInterface>());

            migrations = migrations ?? new List<IEventMigration>(); //We don't want to get any old migrations array that might have been registered by someone else.

            var connectionString =  serviceLocator.Resolve<IConnectionStringProvider>().GetConnectionString(connectionName).ConnectionString;


            if(newContainer.IsTestMode)
            {
                newContainer.Register(CComponent.For<IEventStore>()
                                                .UsingFactoryMethod(sl => new InMemoryEventStore(migrationFactories: migrations))
                                                .Named(registration.StoreName)
                                                .LifestyleSingleton());
            } else
            {
                newContainer.Register(CComponent.For<IEventStore>()
                                                .UsingFactoryMethod(sl => new SqlServerEventStore(connectionString: connectionString, migrations: migrations))
                                                .Named(registration.StoreName)
                                                .LifestyleScoped());
            }


            newContainer.Register(CComponent.For<IEventStoreSession, IUnitOfWorkParticipant>()
                                            .UsingFactoryMethod(oeu => new EventStoreSession(bus: serviceLocator.Resolve<IServiceBus>(),
                                                                                             store: serviceLocator.Resolve<IEventStore>(registration.StoreName),
                                                                                             usageGuard: serviceLocator.Resolve<ISingleContextUseGuard>(),
                                                                                             timeSource: serviceLocator.Resolve<IUtcTimeTimeSource>()))
                                            .Named(registration.SessionImplementationName)
                                            .LifestyleScoped());

            newContainer.Register(CComponent.For<TSessionInterface>(Seq.Create(registration.ReaderType))
                                            .UsingFactoryMethod(locator =>CreateProxyFor<TSessionInterface, TReaderInterface>(locator.Resolve<IEventStoreSession>(registration.SessionImplementationName)))
                                            .Named(registration.SessionName)
                                            .LifestyleScoped());

            return registration;
        }

        static TSessionInterface CreateProxyFor<TSessionInterface, TReaderInterface>(IEventStoreSession session)
            where TSessionInterface : IEventStoreSession
            where TReaderInterface : IEventStoreReader
        {
            var sessionType = EventStoreSessionProxyFactory<TSessionInterface, TReaderInterface>.ProxyType;
            return (TSessionInterface)Activator.CreateInstance(sessionType, new IInterceptor[] { }, session);
        }


        //Using a generic class this way allows us to bypass any need for dictionary lookups or similar giving us excellent performance.
        static class EventStoreSessionProxyFactory<TSessionInterface, TReaderInterface>
            where TSessionInterface : IEventStoreSession
            where TReaderInterface : IEventStoreReader
        {
            internal static readonly Type ProxyType = new DefaultProxyBuilder().CreateInterfaceProxyTypeWithTargetInterface(interfaceToProxy: typeof(IEventStoreSession),
                                                                                                              additionalInterfacesToProxy: new[]
                                                                                                                                           {
                                                                                                                                               typeof(TSessionInterface),
                                                                                                                                               typeof(TReaderInterface)
                                                                                                                                           },
                                                                                                              options: ProxyGenerationOptions.Default);
        }
    }
}
