using System;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Persistence.Common.DependencyInjection;

namespace Composable.Persistence.SqlServer.Messaging.Buses
{
    public class SqlServerTestingEndpointHost : TestingEndpointHostBase
    {
        public static ITestingEndpointHost Create(Func<IRunMode, IDependencyInjectionContainer> containerFactory)
            => new SqlServerTestingEndpointHost(new RunMode(isTesting: true), containerFactory);

        public SqlServerTestingEndpointHost(IRunMode mode, Func<IRunMode, IDependencyInjectionContainer> containerFactory) : base(mode, containerFactory)
        {
        }

        internal override void ExtraEndpointConfiguration(IEndpointBuilder builder)
        {
            builder.RegisterCurrentTestsConfiguredPersistenceLayer();
        }
    }
}