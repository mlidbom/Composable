﻿using System;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Persistence.InMemory.DependencyInjection;
using Composable.Persistence.MySql.DependencyInjection;
using Composable.Persistence.SqlServer.DependencyInjection;
using Composable.Testing;
using Composable.Testing.Performance;

namespace Composable.Persistence.Common.DependencyInjection
{
    public static class TestingPersistenceLayerRegistrar
    {
        public static void RegisterCurrentTestsConfiguredPersistenceLayer(this IEndpointBuilder @this) => RegisterCurrentTestsConfiguredPersistenceLayer(@this.Container, @this.Configuration.ConnectionStringName);

        public static void RegisterCurrentTestsConfiguredPersistenceLayer(this IDependencyInjectionContainer container, string connectionStringName)
        {
            switch(TestEnvironment.TestingPersistenceLayer)
            {
                case PersistenceLayer.SqlServer:
                    container.RegisterSqlServerPersistenceLayer(connectionStringName);
                    break;
                case PersistenceLayer.InMemory:
                    container.RegisterInMemoryPersistenceLayer(connectionStringName);
                    break;
                case PersistenceLayer.MySql:
                    container.RegisterMySqlPersistenceLayer(connectionStringName);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
