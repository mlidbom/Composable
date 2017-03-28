using System;
using AccountManagement.Domain;
using AccountManagement.Domain.Events.EventStore;
using AccountManagement.UI.QueryModels.DocumentDB.Updaters;
using AccountManagement.UI.QueryModels.Services;
using Composable.DependencyInjection;
using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.Tests
{
    public class QueryModelsTestsBase
    {
        protected IServiceLocator ServiceLocator;
        IDisposable _scope;
        protected IAccountManagementQueryModelsReader QueryModelsReader => ServiceLocator.Lease<IAccountManagementQueryModelsReader>().Instance;

        protected void ReplaceContainerScope()
        {
            _scope.Dispose();
            _scope = ServiceLocator.BeginScope();
        }

        [SetUp]
        public void SetupContainerAndScope()
        {

            ServiceLocator = DependencyInjectionContainer.CreateServiceLocatorForTesting(container =>
                                                                                   {
                                                                                       AccountManagementDomainBootstrapper.SetupForTesting(container);
                                                                                       AccountManagementDomainEventStoreBootstrapper.BootstrapForTesting(container);
                                                                                       AccountManagementUiQueryModelsBootstrapper.BootstrapForTesting(container);
                                                                                       AccountManagementUiQueryModelsDocumentDbUpdatersBootstrapper.BootstrapForTesting(container);
                                                                                   });



            _scope = ServiceLocator.BeginScope();
        }

        [TearDown]
        public void DisposeScopeAndContainer()
        {
            _scope.Dispose();
            ServiceLocator.Dispose();
        }
    }
}
