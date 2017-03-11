using AccountManagement.UI.QueryModels.DocumentDbStored;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.CQRS.CQRS.Windsor;
using Composable.Persistence.KeyValueStorage;
using Composable.UnitsOfWork;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.ContainerInstallers
{
    [UsedImplicitly]
    public class AccountManagementDocumentDbReaderInstaller : IWindsorInstaller
    {
        public const string ConnectionStringName = "AccountManagementReadModels";

        public static readonly SqlServerDocumentDbRegistration Registration = new SqlServerDocumentDbRegistration<AccountManagementDocumentDbReaderInstaller>();

        public void Install(
            IWindsorContainer container,
            IConfigurationStore store)
        {
            container.RegisterSqlServerDocumentDb(Registration, ConnectionStringName);

            container.Register(
                Component.For<IAccountManagementDocumentDbQueryModelsReader, IUnitOfWorkParticipant>()
                    .ImplementedBy<AccountManagementDocumentDbQueryModelsReader>()
                    .DependsOn(
                        Registration.DocumentDb,
                        Dependency.OnValue<IDocumentDbSessionInterceptor>(NullOpDocumentDbSessionInterceptor.Instance))
                    .LifestylePerWebRequest()
                );
        }
    }
}
