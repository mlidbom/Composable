﻿using System;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Persistence.SqlServer.DependencyInjection;

namespace Composable.Persistence.SqlServer.Messaging.Buses
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
            //urgent: Move to baseclass and use configured persistence layer name to choose.
            builder.RegisterSqlServerPersistenceLayer();
        }
    }
}