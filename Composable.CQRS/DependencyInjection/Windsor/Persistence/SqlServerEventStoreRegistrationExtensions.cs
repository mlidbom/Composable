using System.Collections.Generic;
using Castle.Windsor;
using Composable.DependencyInjection.Persistence;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Refactoring.Migrations;

namespace Composable.DependencyInjection.Windsor.Persistence
{
    static class WindsorSqlServerEventStoreRegistrationExtensions
    {


        internal static SqlServerEventStoreRegistration RegisterSqlServerEventStore<TSessionInterface, TReaderInterface>(this IWindsorContainer @this,
                                                                                                                         string connectionName,
                                                                                                                         IEnumerable<IEventMigration> migrations = null)
            where TSessionInterface : IEventStoreSession
            where TReaderInterface : IEventStoreReader => @this.AsDependencyInjectionContainer()
                                                               .RegisterSqlServerEventStore<TSessionInterface, TReaderInterface>(connectionName: connectionName,
                                                                                                                                 migrations: migrations);

    }
}
