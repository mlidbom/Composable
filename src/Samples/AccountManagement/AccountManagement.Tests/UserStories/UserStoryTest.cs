using AccountManagement.UserStories.Scenarios;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using NUnit.Framework;

namespace AccountManagement.UserStories
{
    [TestFixture] public abstract class UserStoryTest
    {
        protected ITestingEndpointHost Host;
        internal AccountScenarioApi Scenario => new AccountScenarioApi(Host.ClientEndpoint);

        [SetUp] public void SetupContainerAndBeginScope()
        {
            Host = EndpointHost.Testing.CreateHost(DependencyInjectionContainer.Create);
            new AccountManagementServerDomainBootstrapper().RegisterWith(Host);
        }

        [TearDown] public void Teardown() => Host.Dispose();
    }
}
