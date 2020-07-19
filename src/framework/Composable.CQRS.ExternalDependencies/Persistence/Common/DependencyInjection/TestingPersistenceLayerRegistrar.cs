using System;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Persistence.DB2.DependencyInjection;
using Composable.Persistence.InMemory.DependencyInjection;
using Composable.Persistence.MySql.DependencyInjection;
using Composable.Persistence.MsSql.DependencyInjection;
using Composable.Persistence.Oracle.DependencyInjection;
using Composable.Persistence.PgSql.DependencyInjection;
using Composable.Testing;
using Composable.Testing.Performance;

namespace Composable.Persistence.Common.DependencyInjection
{
    public static class TestingPersistenceLayerRegistrar
    {
        public static void RegisterCurrentTestsConfiguredPersistenceLayer(this IEndpointBuilder @this) => RegisterCurrentTestsConfiguredPersistenceLayer(@this.Container, @this.Configuration.ConnectionStringName);

        public static void RegisterCurrentTestsConfiguredPersistenceLayer(this IDependencyInjectionContainer container, string connectionStringName)
        {
            switch(TestEnv.PersistenceLayer.Current)
            {
                case PersistenceLayer.MsSql:
                    container.RegisterMsSqlPersistenceLayer(connectionStringName);
                    break;
                case PersistenceLayer.InMemory:
                    container.RegisterInMemoryPersistenceLayer(connectionStringName);
                    break;
                case PersistenceLayer.MySql:
                    container.RegisterMySqlPersistenceLayer(connectionStringName);
                    break;
                case PersistenceLayer.PgSql:
                    container.RegisterPgSqlPersistenceLayer(connectionStringName);
                    break;
                case PersistenceLayer.Orcl:
                    container.RegisterOraclePersistenceLayer(connectionStringName);
                    break;
                case PersistenceLayer.DB2:
                    container.RegisterDB2PersistenceLayer(connectionStringName);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
