using System;
using Composable.DependencyInjection;
using Composable.Persistence.Common.DependencyInjection;
using NetMQ;

namespace Composable.Messaging.Buses
{
    public class TestingEndpointHost : TestingEndpointHostBase
    {
        static TestingEndpointHost() => NetMQConfig.MaxSockets *= 20;

        public static ITestingEndpointHost Create(Func<IRunMode, IDependencyInjectionContainer> containerFactory)
            => new TestingEndpointHost(new RunMode(isTesting: true), containerFactory);

        public TestingEndpointHost(IRunMode mode, Func<IRunMode, IDependencyInjectionContainer> containerFactory) : base(mode, containerFactory)
        {
        }

        internal override void ExtraEndpointConfiguration(IEndpointBuilder builder)
        {
            builder.RegisterCurrentTestsConfiguredPersistenceLayer();
        }
    }
}