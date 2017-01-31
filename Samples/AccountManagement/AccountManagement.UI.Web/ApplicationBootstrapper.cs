using System.Web.Mvc;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Composable.ServiceBus;
using JetBrains.Annotations;
using Composable.GenericAbstractions.Time;
using Composable.System.Configuration;
using Composable.Windsor.Testing;

namespace AccountManagement.UI.Web
{
    [UsedImplicitly]
    public class ApplicationBootstrapper
    {
        public static void ConfigureContainer(IWindsorContainer container)
        {
            container.Register(
                Component.For<IUtcTimeTimeSource>().ImplementedBy<DateTimeNowTimeSource>().LifestylePerWebRequest(),
                Component.For<SynchronousBus>().ImplementedBy<SynchronousBus>().LifestylePerWebRequest(),
                Component.For<IAuthenticationContext>().ImplementedBy<AuthenticationContext>().LifestylePerWebRequest()                
                );

            SharedWiring(container);
        }

        static void SharedWiring(IWindsorContainer container)
        {
            container.Register(
                Component.For<IWindsorContainer>().Instance(container),
                Classes.FromThisAssembly().BasedOn<Controller>().WithServiceSelf().LifestyleTransient(),
                Component.For<IConnectionStringProvider>().Instance(new ConnectionStringConfigurationParameterProvider()).LifestyleSingleton()
                );

            container.Install(
                FromAssembly.Containing<Domain.ContainerInstallers.AccountRepositoryInstaller>(),
                FromAssembly.Containing<Domain.Events.EventStore.ContainerInstallers.AccountManagementDomainEventStoreInstaller>(),
                FromAssembly.Containing<UI.QueryModels.ContainerInstallers.AccountManagementDocumentDbReaderInstaller>(),
                FromAssembly.Containing<UI.QueryModels.DocumentDB.Updaters.ContainerInstallers.AccountManagementQuerymodelsSessionInstaller>()
                );
        }

        public static void ConfigureContainerForTests(IWindsorContainer container)
        {
            container.ConfigureWiringForTestsCallBeforeAllOtherWiring();

            SharedWiring(container);
            container.Register(
                Component.For<IServiceBus>().ImplementedBy<SynchronousBus>().LifestylePerWebRequest()
                );


            container.ConfigureWiringForTestsCallAfterAllOtherWiring();
        }
    }
}
