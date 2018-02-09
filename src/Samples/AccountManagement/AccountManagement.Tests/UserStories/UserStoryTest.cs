using AccountManagement.UserStories.Scenarios;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using NUnit.Framework;

namespace AccountManagement.UserStories
{
    [TestFixture] public abstract class UserStoryTest
    {
        protected ITestingEndpointHost Host;
        IEndpoint _clientEndpoint;
        internal AccountScenarioApi Scenario => new AccountScenarioApi(_clientEndpoint);

        [SetUp] public void SetupContainerAndBeginScope()
        {
            Host = EndpointHost.Testing.Create(DependencyInjectionContainer.Create);
            _clientEndpoint = Host.RegisterClientEndpoint();
            new AccountManagementServerDomainBootstrapper().RegisterWith(Host);
            Host.Start();
        }

        [TearDown] public void Teardown() => Host.Dispose();
    }
}
