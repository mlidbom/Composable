using AccountManagement.Domain.Events.EventStore.ContainerInstallers;
using AccountManagement.Domain.Services;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.CQRS.Windsor;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;
using Composable.UnitsOfWork;
using JetBrains.Annotations;

namespace AccountManagement.Domain.ContainerInstallers
{
    [UsedImplicitly]
    public class AccountManagementDomainQuerymodelsSessionInstaller : IWindsorInstaller
    {
        static readonly SqlServerDocumentDbRegistration Registration = new SqlServerDocumentDbRegistration<AccountManagementDomainQuerymodelsSessionInstaller>();

        public void Install(
            IWindsorContainer container,
            IConfigurationStore store)
        {
            container.RegisterSqlServerDocumentDb(Registration, AccountManagementDomainEventStoreInstaller.ConnectionStringName);

            container.Register(
                Component.For<IAccountManagementDomainQueryModelSession, IUnitOfWorkParticipant>()
                    .ImplementedBy<AccountManagementDomainQueryModelSession>()
                    .DependsOn(
                        Registration.DocumentDb,
                        Dependency.OnValue<IDocumentDbSessionInterceptor>(NullOpDocumentDbSessionInterceptor.Instance))
                    .LifestylePerWebRequest()
                );
        }
    }
}
