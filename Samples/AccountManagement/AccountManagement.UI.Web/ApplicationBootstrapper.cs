using System.Web.Mvc;
using AccountManagement.UI.QueryModels.DocumentDB.Updaters.ContainerInstallers;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Composable.CQRS.ServiceBus.NServiceBus;
using Composable.ServiceBus;
using JetBrains.Annotations;
using AccountManagementQuerymodelsSessionInstaller = AccountManagement.UI.QueryModels.DocumentDB.Readers.ContainerInstallers.AccountManagementQuerymodelsSessionInstaller;

namespace AccountManagement.UI.Web
{
    [UsedImplicitly]
    public class ApplicationBootstrapper
    {
        public static void ConfigureContainer(IWindsorContainer container)
        {
            SharedWiring(container);
            container.Register(
                Component.For<NServiceBusServiceBus>().LifestylePerWebRequest(),
                Component.For<SynchronousBus>().ImplementedBy<SynchronousBus>().LifestylePerWebRequest(),
                Component.For<IServiceBus>().ImplementedBy<DualDispatchBus>().LifestylePerWebRequest(),
                Component.For<IAuthenticationContext>().ImplementedBy<AuthenticationContext>()
                );
        }

        private static void SharedWiring(IWindsorContainer container)
        {
            container.Register(
                Component.For<IWindsorContainer>().Instance(container),
                Classes.FromThisAssembly().BasedOn<Controller>().WithServiceSelf().LifestyleTransient()
                );

            container.Install(
                FromAssembly.Containing<Domain.ContainerInstallers.AccountManagementDomainEventStoreInstaller>(),
                FromAssembly.Containing<AccountManagementQuerymodelsSessionInstaller>(),
                FromAssembly.Containing<QueryModels.DocumentDB.Updaters.ContainerInstallers.AccountManagementQuerymodelsSessionInstaller>()
                );
        }

        public static void ConfigureContainerForTests(IWindsorContainer container)
        {
            SharedWiring(container);
            container.Register(
                Component.For<IServiceBus>().ImplementedBy<SynchronousBus>().LifestylePerWebRequest()
                );
        }
    }
}
