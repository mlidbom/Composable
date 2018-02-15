using System;
using System.Threading.Tasks;
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

        [SetUp] public async Task SetupContainerAndBeginScope()
        {
            Host = EndpointHost.Testing.Create(DependencyInjectionContainer.Create);
            new AccountManagementServerDomainBootstrapper().RegisterWith(Host);
            _clientEndpoint = Host.RegisterTestingEndpoint("Client", setup: builder => AccountManagementApiTypeMapper.MapTypes(builder.TypeMapper));
            await Host.StartAsync();
        }

        [TearDown] public void Teardown() => Host.Dispose();
    }
}
