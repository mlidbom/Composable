using System.Web.Mvc;
using AccountManagement.UI.QueryModels.ContainerInstallers;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Composable.CQRS.ServiceBus.NServiceBus;
using Composable.ServiceBus;
using JetBrains.Annotations;

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
                FromAssembly.Containing<Domain.ContainerInstallers.AccountRepositoryInstaller>(),
                FromAssembly.Containing<Domain.Events.EventStore.ContainerInstallers.AccountManagementDomainEventStoreInstaller>(),
                FromAssembly.Containing<AccountManagementDocumentDbReaderInstaller>(),
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
