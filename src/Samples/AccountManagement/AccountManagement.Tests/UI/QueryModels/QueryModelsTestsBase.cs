using System;
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
        ITestingEndpointHost _host;
        IEndpoint _domainEndpoint;
        protected IAccountManagementQueryModelsReader QueryModelsReader => ServiceLocator.Lease<IAccountManagementQueryModelsReader>().Instance;

        protected void ReplaceContainerScope()
        {
            _scope.Dispose();
            _scope = ServiceLocator.BeginScope();
        }

        [SetUp]
        public void SetupContainerAndScope()
        {
            _host = EndpointHost.Testing.CreateHost(DependencyInjectionContainer.Create);
            _domainEndpoint = AccountManagementServerDomainBootstrapper.RegisterWith(_host);

            ServiceLocator = _domainEndpoint.ServiceLocator;

            _scope = ServiceLocator.BeginScope();
        }

        [TearDown]
        public void DisposeScopeAndContainer()
        {
            _scope.Dispose();
            _host.Dispose();
        }
    }
}
