using System.Web.Mvc;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Composable.CQRS.Testing.Windsor.Testing;
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
                Component.For<IMessageHandlerRegistrar>().ImplementedBy<MessageHandlerRegistry>().LifestyleSingleton(),
                Component.For<IServiceBus>().ImplementedBy<TestingOnlyServiceBus>().LifestylePerWebRequest(),
                Component.For<IAuthenticationContext>().ImplementedBy<AuthenticationContext>().LifestylePerWebRequest(),
                Component.For<IWindsorContainer>().Instance(container),
                Component.For<IConnectionStringProvider>().Instance(new ConnectionStringConfigurationParameterProvider()).LifestyleSingleton()
                );

            SharedWiring(container);
        }

        static void SharedWiring(IWindsorContainer container)
        {
            container.Register(                
                Classes.FromThisAssembly().BasedOn<Controller>().WithServiceSelf().LifestyleTransient()                
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
            container.SetupForTesting(SharedWiring);
        }
    }
}
