using System;
using System.Collections.Generic;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.Contracts;
using Composable.CQRS.CQRS.EventSourcing;
using Composable.CQRS.CQRS.EventSourcing.MicrosoftSQLServer;
using Composable.CQRS.CQRS.EventSourcing.Refactoring.Migrations;
using Composable.CQRS.CQRS.EventSourcing.Refactoring.Naming;
using Composable.CQRS.Windsor.Testing;
using Composable.System.Configuration;
using Composable.System.Linq;
using Composable.UnitsOfWork;
using Composable.Windsor.Testing;

// ReSharper disable UnusedMember.Global todo: write complete tests and remove unused.

namespace Composable.CQRS.CQRS.Windsor
{
    public abstract class SqlServerEventStoreRegistration
    {
        protected SqlServerEventStoreRegistration(string description, Type sessionImplementor, Type sessionType, Type readerType)
        {
            Contract.Argument(() => description)
                        .NotNullEmptyOrWhiteSpace();

            SessionType = sessionType;
            ReaderType = readerType;
            SessionImplementor = sessionImplementor;
            StoreName = $"{description}.Store";
            SessionName = $"{description}.Session";
        }

        internal Type ReaderType { get; }
        internal Type SessionType { get; }
        internal Type SessionImplementor { get; private set; }
        internal string StoreName { get; }
        internal string SessionName { get; }
        internal ServiceOverride Store => Dependency.OnComponent(typeof(IEventStore), componentName: StoreName);
        public ServiceOverride Session => Dependency.OnComponent(SessionType, componentName: SessionName);
        public ServiceOverride Reader => Dependency.OnComponent(ReaderType, componentName: SessionName);

    }

    class SqlServerEventStoreRegistration<TFactory> : SqlServerEventStoreRegistration
    {
        public SqlServerEventStoreRegistration() : base(typeof(TFactory).FullName,sessionImplementor: typeof(EventStoreSession), sessionType: typeof(IEventStoreSession), readerType: typeof(IEventStoreReader)) {}
    }

    class SqlServerEventStoreRegistration<TSessionClass, TSessionInterface, TReaderInterface> : SqlServerEventStoreRegistration
        where TSessionClass : EventStoreSession
        where TSessionInterface : IEventStoreSession
        where TReaderInterface : IEventStoreReader
    {
        public SqlServerEventStoreRegistration() : base(typeof(TSessionClass).FullName, sessionImplementor: typeof(TSessionClass), sessionType: typeof(TSessionInterface), readerType: typeof(TReaderInterface)) { }
    }

    public static class SqlServerEventStoreRegistrationExtensions
    {
        public static SqlServerEventStoreRegistration RegisterSqlServerEventStore<TSessionClass, TSessionInterface, TReaderInterface>
            (this IWindsorContainer @this,
             string connectionName,
             Dependency nameMapper = null,
             Dependency migrations = null)
            where TSessionClass : EventStoreSession
            where TSessionInterface : IEventStoreSession
            where TReaderInterface : IEventStoreReader
        {

            return @this.RegisterSqlServerEventStore(
                registration: new SqlServerEventStoreRegistration<TSessionClass, TSessionInterface, TReaderInterface>(),
                connectionName: connectionName,
                nameMapper: nameMapper,
                migrations: migrations
                );
        }

        internal static SqlServerEventStoreRegistration RegisterSqlServerEventStore
            (this IWindsorContainer @this,
             SqlServerEventStoreRegistration registration,
             string connectionName,
             Dependency nameMapper = null,
             Dependency migrations = null)
        {
            Contract.Argument(() => registration)
                        .NotNull();
            Contract.Argument(() => connectionName)
                        .NotNullEmptyOrWhiteSpace();

            nameMapper = nameMapper ?? Dependency.OnValue<IEventNameMapper>(null);//We don't want to get any old name mapper that might have been registered by someone else.
            migrations = migrations ?? Dependency.OnValue<IEnumerable<IEventMigration>>(null); //We don't want to get any old migrations array that might have been registered by someone else.

            var connectionString = Dependency.OnValue(typeof(string),@this.Resolve<IConnectionStringProvider>().GetConnectionString(connectionName).ConnectionString);

            @this.Register(
                Component.For<IEventStore>()
                         .ImplementedBy<SqlServerEventStore>()
                         .DependsOn(connectionString, nameMapper, migrations)
                    .LifestylePerWebRequest()
                    .Named(registration.StoreName),
                Component.For(Seq.Create(registration.SessionType, registration.ReaderType, typeof(IUnitOfWorkParticipant)))
                         .ImplementedBy(registration.SessionImplementor)
                         .DependsOn(registration.Store)
                         .LifestylePerWebRequest()
                         .Named(registration.SessionName)
                );

            @this.WhenTesting()
                 .ReplaceEventStore(registration.StoreName);

            return registration;
        }
    }
}
