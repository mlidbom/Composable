﻿using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.EventStore;
using Composable.Persistence.MsSql.DocumentDb;
using Composable.Persistence.MsSql.EventStore;
using Composable.Persistence.MsSql.Messaging.Buses.Implementation;
using Composable.Persistence.MsSql.SystemExtensions;
using Composable.Persistence.MsSql.Testing.Databases;
using Composable.Refactoring.Naming;
using Composable.System.Configuration;

namespace Composable.Persistence.MsSql.DependencyInjection
{
    public static class MsSqlPersistenceLayerRegistrar
    {
       public static void RegisterMsSqlPersistenceLayer(this IEndpointBuilder @this)
        {
            var container = @this.Container;
            var configurationConnectionStringName = @this.Configuration.ConnectionStringName;

            RegisterMsSqlPersistenceLayer(container, configurationConnectionStringName);
        }

        //todo: does the fact that we register all this stuff using a connectionStringName mean that, using named components, we could easily have multiple registrations as long as they use different connectionStrings
        public static void RegisterMsSqlPersistenceLayer(this IDependencyInjectionContainer container, string connectionStringName)
        {
            //Connection management
            if(container.RunMode.IsTesting)
            {
                container.Register(Singleton.For<MsSqlDatabasePool>()
                                            .CreatedBy(((IConfigurationParameterProvider configurationParameterProvider) => new MsSqlDatabasePool()))
                                            .DelegateToParentServiceLocatorWhenCloning());

                container.Register(
                    Singleton.For<IMsSqlConnectionProvider>()
                             .CreatedBy((MsSqlDatabasePool pool) => new MsSqlConnectionProvider(pool.ConnectionStringFor(connectionStringName)))
                );
            } else
            {
                container.Register(
                    Singleton.For<IMsSqlConnectionProvider>()
                             .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => new MsSqlConnectionProvider(configurationParameterProvider.GetString(connectionStringName)))
                             .DelegateToParentServiceLocatorWhenCloning());
            }


            //Service bus
            //Bug: Urgent: Registering these as Scoped does not cause a failure even though the endpoint builder wires singletons to use them. Strangely, doing the same with the in-memory version does cause a failure!
            container.Register(
                Singleton.For<IServiceBusPersistenceLayer.IOutboxPersistenceLayer>()
                         .CreatedBy((IMsSqlConnectionProvider endpointSqlConnection) => new MsSqlOutboxPersistenceLayer(endpointSqlConnection)),
                Singleton.For<IServiceBusPersistenceLayer.IInboxPersistenceLayer>()
                         .CreatedBy((IMsSqlConnectionProvider endpointSqlConnection) => new MsSqlInboxPersistenceLayer(endpointSqlConnection)));

            //DocumentDB
            container.Register(
                Singleton.For<IDocumentDbPersistenceLayer>()
                         .CreatedBy((IMsSqlConnectionProvider connectionProvider) => new MsSqlDocumentDbPersistenceLayer(connectionProvider)));

            //Event store
            container.Register(
                Singleton.For<MsSqlEventStoreConnectionManager>()
                         .CreatedBy((IMsSqlConnectionProvider sqlConnectionProvider) => new MsSqlEventStoreConnectionManager(sqlConnectionProvider)),
                Singleton.For<IEventStorePersistenceLayer>()
                         .CreatedBy((MsSqlEventStoreConnectionManager connectionManager, ITypeMapper typeMapper) => new MsSqlEventStorePersistenceLayer(connectionManager)));
        }
    }
}
