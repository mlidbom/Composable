using System;
using Composable.DependencyInjection;

namespace Composable.Messaging.Buses
{
    public class SqlServerTestingEndpointHost : TestingEndpointHost
    {
        public static ITestingEndpointHost Create(Func<IRunMode, IDependencyInjectionContainer> containerFactory, TestingMode mode = TestingMode.DatabasePool)
            => new SqlServerTestingEndpointHost(new RunMode(isTesting: true, testingMode: mode), containerFactory);

        public SqlServerTestingEndpointHost(IRunMode mode, Func<IRunMode, IDependencyInjectionContainer> containerFactory) : base(mode, containerFactory)
        {
        }

        internal override void ExtraEndpointConfiguration(IEndpointBuilder builder)
        {
            builder.RegisterSqlServerPersistenceLayer();
        }
    }
}