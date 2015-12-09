using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.MicrosoftSQLServer;
using Composable.CQRS.EventSourcing.Refactoring.Migrations;
using Composable.CQRS.EventSourcing.Refactoring.Naming;
using Composable.System;
using Composable.System.Configuration;
using Composable.UnitsOfWork;
using Composable.Windsor.Testing;

namespace Composable.CQRS.Windsor
{
    public abstract class SqlServerEventStoreRegistration
    {
        protected SqlServerEventStoreRegistration(string description)
        {
            Contract.Requires(!description.IsNullOrWhiteSpace());
            StoreName = $"{description}.Store";
            SessionName = $"{description}.Session";
        }
        internal string StoreName { get; }
        internal string SessionName { get; }
        public Dependency Store => Dependency.OnComponent(typeof(IEventStore), componentName: StoreName);
        public Dependency Session => Dependency.OnComponent(typeof(IEventStoreSession), componentName: SessionName);
        public Dependency Reader => Dependency.OnComponent(typeof(IEventStoreReader), componentName: SessionName);

    }

    public class SqlServerEventStoreRegistration<TFactory> : SqlServerEventStoreRegistration
    {
        public SqlServerEventStoreRegistration() : base(typeof(TFactory).FullName) {}
    }

    public static class SqlServerEventStoreRegistrationExtensions
    {
        public static SqlServerEventStoreRegistration RegisterSqlServerEventStore
            (this IWindsorContainer @this,
             SqlServerEventStoreRegistration registration,
             string connectionName,
             Dependency nameMapper = null,
             Dependency migrations = null)
        {
            Contract.Requires(registration != null);
            Contract.Requires(!connectionName.IsNullOrWhiteSpace());

            nameMapper = nameMapper ?? Dependency.OnValue<IEventNameMapper>(null);//We don't want to get any old name mapper that might have been registered by someone else.
            migrations = migrations ?? Dependency.OnValue<IEnumerable<IEventMigration>>(null); //We don't want to get any old migrations array that might have been registered by someone else.

            var connectionString = Dependency.OnValue(typeof(string),@this.Resolve<IConnectionStringProvider>().GetConnectionString(connectionName).ConnectionString);

            @this.Register(
                Component.For<IEventStore>()
                         .ImplementedBy<SqlServerEventStore>()
                         .DependsOn(connectionString, nameMapper, migrations)
                    .LifestylePerWebRequest()
                    .Named(registration.StoreName),
                Component.For<IEventStoreSession, IEventStoreReader, IUnitOfWorkParticipant>()
                         .ImplementedBy<EventStoreSession>()
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
