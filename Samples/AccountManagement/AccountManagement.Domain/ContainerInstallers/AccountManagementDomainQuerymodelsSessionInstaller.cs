using AccountManagement.Domain.Services;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;
using Composable.System.Configuration;

namespace AccountManagement.Domain.ContainerInstallers
{
    public class AccountManagementDomainQuerymodelsSessionInstaller : IWindsorInstaller
    {
        public static class ComponentKeys
        {
            public const string KeyForDocumentDb = "AccountManagement.Domain.QueryModels.IDocumentDb";
            public const string KeyForInMemoryDocumentDb = "AccountManagement.Domain.QueryModels.IDocumentDb.InMemory";
            public const string KeyForSession = "AccountManagement.Domain.QueryModels.IDocumentDbSession";
            public const string KeyForNullOpSessionInterceptor = "AccountManagement.Domain.QueryModels.NullOpSessionInterceptor";
        }

        public static string ConnectionStringName { get { return AccountManagementDomainEventStoreInstaller.ConnectionStringName; } }

        public void Install(
            IWindsorContainer container,
            IConfigurationStore store)
        {
            container.Register(
                Component.For<IDocumentDb>()
                    .ImplementedBy<SqlServerDocumentDb>()
                    .DependsOn(new {connectionString = GetConnectionStringFromConfiguration(ConnectionStringName)})
                    .Named(ComponentKeys.KeyForDocumentDb)
                    .LifestylePerWebRequest(),
                Component.For<IDocumentDbSessionInterceptor>()
                    .Instance(NullOpDocumentDbSessionInterceptor.Instance)
                    .Named(ComponentKeys.KeyForNullOpSessionInterceptor)
                    .LifestyleSingleton(),
                Component.For<IDocumentDbSession, IAccountManagementDomainQueryModelSession>()
                    .ImplementedBy<AccountManagementDomainQueryModelSession>()
                    .DependsOn(
                        Dependency.OnComponent(typeof(IDocumentDb), ComponentKeys.KeyForDocumentDb),
                        Dependency.OnComponent(typeof(IDocumentDbSessionInterceptor), ComponentKeys.KeyForNullOpSessionInterceptor))
                    .Named(ComponentKeys.KeyForSession)
                    .LifestylePerWebRequest()
                );
        }

        private static string GetConnectionStringFromConfiguration(string key)
        {
            return new ConnectionStringConfigurationParameterProvider().GetConnectionString(key).ConnectionString;
        }
    }
}
