using System;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Persistence.InMemory.DependencyInjection;
using Composable.Persistence.MySql.DependencyInjection;
using Composable.Persistence.SqlServer.DependencyInjection;

namespace Composable.Persistence.Common.DependencyInjection
{
    public static class TestingPersistenceLayerRegistrar
    {
        public static void RegisterCurrentTestsConfiguredPersistenceLayer(this IEndpointBuilder @this)
        {
            switch(@this.Container.RunMode.TestingPersistenceLayer)
            {
                case PersistenceLayer.SqlServer:
                    @this.RegisterSqlServerPersistenceLayer();
                    break;
                case PersistenceLayer.InMemory:
                    @this.RegisterInMemoryPersistenceLayer();
                    break;
                case PersistenceLayer.MySql:
                    @this.RegisterMySqlPersistenceLayer();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
