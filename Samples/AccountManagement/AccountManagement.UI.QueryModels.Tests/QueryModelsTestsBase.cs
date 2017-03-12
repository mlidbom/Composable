using System;
using AccountManagement.UI.QueryModels.Services;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Composable.CQRS.Windsor.Testing.Testing;
using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.Tests
{
    public class QueryModelsTestsBase
    {
        protected WindsorContainer Container;
        IDisposable _scope;
        protected IAccountManagementQueryModelsReader QueryModelsReader => Container.Resolve<IAccountManagementQueryModelsReader>();

        protected void ReplaceContainerScope()
        {
            _scope.Dispose();
            _scope = Container.BeginScope();
        }

        [SetUp]
        public void SetupContainerAndScope()
        {
            Container = new WindsorContainer();

            Container.SetupForTesting(container =>
                                      {
                                          container.Install(
                                                            FromAssembly.Containing<Domain.ContainerInstallers.AccountRepositoryInstaller>(),
                                                            FromAssembly.Containing<Domain.Events.EventStore.ContainerInstallers.AccountManagementDomainEventStoreInstaller>(),
                                                            FromAssembly.Containing<UI.QueryModels.ContainerInstallers.AccountManagementDocumentDbReaderInstaller>(),
                                                            FromAssembly
                                                                .Containing
                                                                <UI.QueryModels.DocumentDB.Updaters.ContainerInstallers.AccountManagementQuerymodelsSessionInstaller>()
                                                           );

                                      });

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
