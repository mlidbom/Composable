using System;
using AccountManagement.Domain;
using AccountManagement.Domain.Events.EventStore;
using AccountManagement.UI.QueryModels.DocumentDB.Updaters;
using AccountManagement.UI.QueryModels.Services;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Windsor;
using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.Tests
{
    public class QueryModelsTestsBase
    {
        protected IServiceLocator Container;
        IDisposable _scope;
        protected IAccountManagementQueryModelsReader QueryModelsReader => Container.Lease<IAccountManagementQueryModelsReader>().Instance;

        protected void ReplaceContainerScope()
        {
            _scope.Dispose();
            _scope = Container.BeginScope();
        }

        [SetUp]
        public void SetupContainerAndScope()
        {

            Container = WindsorDependencyInjectionContainerFactory.SetupForTesting(container =>
                                                                                   {
                                                                                       AccountManagementDomainBootstrapper.SetupForTesting(container);
                                                                                       AccountManagementDomainEventStoreBootstrapper.BootstrapForTesting(container);
                                                                                       AccountManagementUiQueryModelsBootstrapper.BootstrapForTesting(container);
                                                                                       AccountManagementUiQueryModelsDocumentDbUpdatersBootstrapper.BootstrapForTesting(container);
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
