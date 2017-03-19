using System;
using System.Collections.Generic;
using Castle.DynamicProxy;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.Contracts;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.MicrosoftSQLServer;
using Composable.CQRS.EventSourcing.Refactoring.Migrations;
using Composable.CQRS.EventSourcing.Refactoring.Naming;
using Composable.System.Configuration;
using Composable.System.Linq;
using Composable.UnitsOfWork;
using Composable.Windsor.Testing;

namespace Composable.CQRS.Windsor
{
    public abstract class SqlServerEventStoreRegistration
    {
        protected SqlServerEventStoreRegistration(string description, Type sessionType, Type readerType)
        {
            SessionType = sessionType;
            ReaderType = readerType;
            StoreName = $"{description}.Store";
            SessionName = $"{description}.Session";
            SessionImplementationName = $"{description}.SessionImplementation";
        }

        internal string SessionImplementationName { get; }
        internal Type ReaderType { get; }
        internal Type SessionType { get; }
        internal string StoreName { get; }
        internal string SessionName { get; }
        internal ServiceOverride Store => Dependency.OnComponent(typeof(IEventStore), componentName: StoreName);

    }

    class SqlServerEventStoreRegistration<TSessionInterface, TReaderInterface> : SqlServerEventStoreRegistration
        where TSessionInterface : IEventStoreSession
        where TReaderInterface : IEventStoreReader
    {
        public SqlServerEventStoreRegistration() : base(typeof(TSessionInterface).FullName, sessionType: typeof(TSessionInterface), readerType: typeof(TReaderInterface)) { }
    }

    public static class SqlServerEventStoreRegistrationExtensions
    {
        public static SqlServerEventStoreRegistration RegisterSqlServerEventStore<TSessionInterface, TReaderInterface>
            (this IWindsorContainer @this,
             string connectionName,
             Dependency nameMapper = null,
             Dependency migrations = null)
            where TSessionInterface : IEventStoreSession
            where TReaderInterface : IEventStoreReader => @this.RegisterSqlServerEventStore<TSessionInterface, TReaderInterface>(
                                                                                            registration: new SqlServerEventStoreRegistration<TSessionInterface, TReaderInterface>(),
                                                                                            connectionName: connectionName,
                                                                                            nameMapper: nameMapper,
                                                                                            migrations: migrations
                                                                                           );

        static SqlServerEventStoreRegistration RegisterSqlServerEventStore<TSessionInterface, TReaderInterface>
            (this IWindsorContainer @this,
             SqlServerEventStoreRegistration registration,
             string connectionName,
             Dependency nameMapper = null,
             Dependency migrations = null)
            where TSessionInterface : IEventStoreSession
            where TReaderInterface : IEventStoreReader
        {
            Contract.Argument(() => registration)
                        .NotNull();
            Contract.Argument(() => connectionName)
                        .NotNullEmptyOrWhiteSpace();

            GeneratedLowLevelInterfaceInspector.InspectInterfaces(Seq.OfTypes<TSessionInterface, TReaderInterface>());

            nameMapper = nameMapper ?? Dependency.OnValue<IEventNameMapper>(null);//We don't want to get any old name mapper that might have been registered by someone else.
            migrations = migrations ?? Dependency.OnValue<IEnumerable<IEventMigration>>(null); //We don't want to get any old migrations array that might have been registered by someone else.

            var connectionString = Dependency.OnValue(typeof(string),@this.Resolve<IConnectionStringProvider>().GetConnectionString(connectionName).ConnectionString);

            @this.Register(
                Component.For<IEventStore>()
                         .ImplementedBy<SqlServerEventStore>()
                         .DependsOn(connectionString, nameMapper, migrations)
                    .LifestylePerWebRequest()
                    .Named(registration.StoreName),
                Component.For<IEventStoreSession,IUnitOfWorkParticipant>()
                         .ImplementedBy(typeof(EventStoreSession))
                         .DependsOn(registration.Store)
                         .LifestylePerWebRequest()
                         .Named(registration.SessionImplementationName),
                Component.For(Seq.Create(registration.SessionType, registration.ReaderType))
                         .UsingFactoryMethod(kernel => CreateProxyFor<TSessionInterface, TReaderInterface>(kernel.Resolve<IEventStoreSession>(registration.SessionImplementationName)))
                         .LifestylePerWebRequest()
                         .Named(registration.SessionName)
                );

            @this.WhenTesting()
                 .ReplaceEventStore(registration.StoreName);

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
