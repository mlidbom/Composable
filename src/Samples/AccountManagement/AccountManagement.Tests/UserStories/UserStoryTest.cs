using System;
using System.Threading.Tasks;
using AccountManagement.API;
using AccountManagement.UserStories.Scenarios;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Persistence.MsSql.Messaging.Buses;
using NUnit.Framework;

namespace AccountManagement.UserStories
{
    [TestFixture] public abstract class UserStoryTest
    {
        protected ITestingEndpointHost Host;
        IEndpoint _clientEndpoint;
        internal AccountScenarioApi Scenario => new AccountScenarioApi(_clientEndpoint);

        [SetUp] public async Task SetupContainerAndBeginScope()
        {
            Host = TestingEndpointHost.Create(DependencyInjectionContainer.Create);
            new AccountManagementServerDomainBootstrapper().RegisterWith(Host);
            _clientEndpoint = Host.RegisterTestingEndpoint(setup:AccountApi.RegisterWithClientEndpoint);
            await Host.StartAsync();
        }

        [TearDown] public void Teardown() => Host.Dispose();
    }
}
