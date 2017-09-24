using Composable.DependencyInjection;

namespace Composable.Messaging.Buses
{
    class TestingEndpointHost : EndpointHost, ITestingEndpointHost
    {
        public TestingEndpointHost(IRunMode mode) : base(mode) {}
    }
}
