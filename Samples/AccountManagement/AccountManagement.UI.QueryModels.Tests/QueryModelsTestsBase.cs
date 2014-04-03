using System;
using AccountManagement.UI.QueryModels.ContainerInstallers;
using AccountManagement.UI.QueryModels.Services;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Composable.CQRS.Windsor.Testing;
using Composable.ServiceBus;
using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.Tests
{
    public class QueryModelsTestsBase
    {
        protected WindsorContainer Container;
        private IDisposable _scope;
        protected IAccountManagementQueryModelsReader QueryModelsReader { get { return Container.Resolve<IAccountManagementQueryModelsReader>(); } }

        [SetUp]
        public void SetupContainerAndScope()
        {
            Container = new WindsorContainer();
            Container.ConfigureWiringForTestsCallBeforeAllOtherWiring();
            Container.Install(
                FromAssembly.Containing<Domain.ContainerInstallers.AccountRepositoryInstaller>(),
                FromAssembly.Containing<Domain.Events.EventStore.ContainerInstallers.AccountManagementDomainEventStoreInstaller>(),
                FromAssembly.Containing<UI.QueryModels.ContainerInstallers.AccountManagementDocumentDbReaderInstaller>(),
                FromAssembly.Containing<UI.QueryModels.DocumentDB.Updaters.ContainerInstallers.AccountManagementQuerymodelsSessionInstaller>()
                );

            Container.Register(
                Component.For<IWindsorContainer>().Instance(Container),
                Component.For<IServiceBus>().ImplementedBy<SynchronousBus>()
                );

            Container.ConfigureWiringForTestsCallAfterAllOtherWiring();
            _scope = Container.BeginScope();
        }

        [TearDown]
        public void DisposeScopeAndContainer()
        {
            _scope.Dispose();
            Container.Dispose();
        }
    }
}
