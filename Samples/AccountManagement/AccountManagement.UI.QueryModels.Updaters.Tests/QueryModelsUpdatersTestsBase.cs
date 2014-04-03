using System;
using AccountManagement.Domain.Events.EventStore;
using AccountManagement.Domain.Events.EventStore.ContainerInstallers;
using AccountManagement.UI.QueryModels.DocumentDB.Readers.Services;
using AccountManagement.UI.QueryModels.DocumentDB.Updaters.ContainerInstallers;
using AccountManagement.UI.QueryModels.Services;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Composable.CQRS.Windsor.Testing;
using Composable.ServiceBus;
using NUnit.Framework;
using AccountManagementQuerymodelsSessionInstaller = AccountManagement.UI.QueryModels.DocumentDB.Readers.ContainerInstallers.AccountManagementQuerymodelsSessionInstaller;

namespace AccountManagement.UI.QueryModels.DocumentDB.Updaters.Tests
{
    public class QueryModelsUpdatersTestsBase
    {
        protected WindsorContainer Container;
        private IDisposable _scope;
        protected IAccountManagementQueryModelsReader Session { get { return Container.Resolve<IAccountManagementQueryModelsReader>(); } }

        [SetUp]
        public void SetupContainerAndScope()
        {
            Container = new WindsorContainer();
            Container.Kernel.Resolver.AddSubResolver(new CollectionResolver(Container.Kernel));
            Container.ConfigureWiringForTestsCallBeforeAllOtherWiring();
            Container.Install(
                FromAssembly.Containing<Domain.ContainerInstallers.AccountRepositoryInstaller>(),
                FromAssembly.Containing<AccountManagementDomainEventStoreInstaller>(),
                FromAssembly.Containing<AccountManagementQuerymodelsSessionInstaller>(),
                FromAssembly.Containing<Updaters.ContainerInstallers.AccountManagementQuerymodelsSessionInstaller>()
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
