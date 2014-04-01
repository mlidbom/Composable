using AccountManagement.UI.QueryModels.Updaters.Services;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;
using Composable.System.Configuration;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.Updaters.ContainerInstallers
{
    [UsedImplicitly]
    public class AccountManagementQuerymodelsSessionInstaller : IWindsorInstaller
    {
        public static class ComponentKeys
        {
            public const string KeyForDocumentDb = "AccountManagement.QueryModelUpdaters.IDocumentDb";
            public const string KeyForSession = "AccountManagement.QueryModelUpdaters.IDocumentDbSession";
            public const string KeyForNullOpSessionInterceptor = "AccountManagement.QueryModelUpdaters.NullOpSessionInterceptor";
        }

        public const string ConnectionStringName = QueryModels.ContainerInstallers.AccountManagementQuerymodelsSessionInstaller.ConnectionStringName;

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
                Component.For<IDocumentDbSession, IAccountManagementQueryModelUpdaterSession>()
                    .ImplementedBy<AccountManagementQueryModelUpdaterSession>()
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
