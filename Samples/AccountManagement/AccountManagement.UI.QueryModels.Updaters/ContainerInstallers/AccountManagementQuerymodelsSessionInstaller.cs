using AccountManagement.UI.QueryModels.DocumentDB.Updaters.Services;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;
using Composable.System.Configuration;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.DocumentDB.Updaters.ContainerInstallers
{
    [UsedImplicitly]
    public class AccountManagementQuerymodelsSessionInstaller : IWindsorInstaller
    {
        public static class ComponentKeys
        {
            public const string KeyForDocumentDb = "AccountManagement.QueryModelUpdaters.IDocumentDb";
            public const string KeyForSession = "AccountManagement.QueryModelUpdaters.IDocumentDbSession";
        }

        public const string ConnectionStringName = Readers.ContainerInstallers.AccountManagementQuerymodelsSessionInstaller.ConnectionStringName;

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
                Component.For<IDocumentDbSession, IAccountManagementQueryModelUpdaterSession>()
                    .ImplementedBy<AccountManagementQueryModelUpdaterSession>()
                    .DependsOn(
                        Dependency.OnComponent(typeof(IDocumentDb), ComponentKeys.KeyForDocumentDb),
                        Dependency.OnValue<IDocumentDbSessionInterceptor>(NullOpDocumentDbSessionInterceptor.Instance))
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
