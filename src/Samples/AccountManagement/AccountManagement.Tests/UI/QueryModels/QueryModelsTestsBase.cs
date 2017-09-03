using System;
using AccountManagement.Domain;
using AccountManagement.UI.QueryModels;
using AccountManagement.UI.QueryModels.DocumentDB.Updaters;
using AccountManagement.UI.QueryModels.Services;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using NUnit.Framework;

namespace AccountManagement.Tests.UI.QueryModels
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
                                                                                       AccountManagementDomainBootstrapper.SetupContainer(container);
                                                                                       AccountManagementUiQueryModelsBootstrapper.SetupContainer(container);
                                                                                       AccountManagementUiQueryModelsDocumentDbUpdatersBootstrapper.SetupContainer(container);
                                                                                   });

            ServiceLocator.Use<IMessageHandlerRegistrar>(registrar =>
                                                         {
                                                             AccountManagementDomainBootstrapper.RegisterHandlers(registrar, ServiceLocator);
                                                             AccountManagementUiQueryModelsBootstrapper.RegisterHandlers(registrar, ServiceLocator);
                                                             AccountManagementUiQueryModelsDocumentDbUpdatersBootstrapper.RegisterHandlers(registrar, ServiceLocator);
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
