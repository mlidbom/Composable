using AccountManagement.UI.QueryModels.ContainerInstallers;
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
        public void Install(
            IWindsorContainer container,
            IConfigurationStore store)
        {


            container.Register(
                Component.For<IDocumentDbSession, IAccountManagementQueryModelUpdaterSession>()
                    .ImplementedBy<AccountManagementQueryModelUpdaterSession>()
                    .DependsOn(
                        AccountManagementDocumentDbReaderInstaller.Registration.DocumentDb,
                        Dependency.OnValue<IDocumentDbSessionInterceptor>(NullOpDocumentDbSessionInterceptor.Instance))
                    .LifestylePerWebRequest()
                );
        }

        private static string GetConnectionStringFromConfiguration(string key)
        {
            return new ConnectionStringConfigurationParameterProvider().GetConnectionString(key).ConnectionString;
        }
    }
}
