using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using NUnit.Framework;

namespace AccountManagement.Tests.Domain
{
    [TestFixture] public abstract class AccountManagementTestBase
    {
        protected ITestingEndpointHost Host;
        protected IEndpoint ClientEndpoint => Host.ClientEndpoint;

        [SetUp] public void SetupContainerAndBeginScope()
        {
            Host = EndpointHost.Testing.CreateHost(DependencyInjectionContainer.Create);
            new AccountManagementServerDomainBootstrapper().RegisterWith(Host);
        }

        [TearDown] public void Teardown() => Host.Dispose();
    }
}
