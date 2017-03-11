using AccountManagement.UI.QueryModels.ContainerInstallers;
using AccountManagement.UI.QueryModels.DocumentDB.Updaters.Services;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.Persistence.KeyValueStorage;
using Composable.UnitsOfWork;
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
                Component.For<IAccountManagementQueryModelUpdaterSession, IUnitOfWorkParticipant>()
                    .ImplementedBy<AccountManagementQueryModelUpdaterSession>()
                    .DependsOn(
                        AccountManagementDocumentDbReaderInstaller.Registration.DocumentDb,
                        Dependency.OnValue<IDocumentDbSessionInterceptor>(NullOpDocumentDbSessionInterceptor.Instance))
                    .LifestylePerWebRequest()
                );
        }
    }
}
