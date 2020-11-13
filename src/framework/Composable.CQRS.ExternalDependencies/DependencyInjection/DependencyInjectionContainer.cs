using System;
using Composable.DependencyInjection.SimpleInjector;
using Composable.DependencyInjection.Windsor;
using Composable.Messaging.Buses;
using Composable.Persistence.Common.DependencyInjection;
using Composable.Testing;
using JetBrains.Annotations;

namespace Composable.DependencyInjection
{
    public static class DependencyInjectionContainer
    {
        public static IServiceLocator CreateServiceLocatorForTesting([InstantHandle]Action<IEndpointBuilder> setup)
        {
            var host = TestingEndpointHost.Create(Create);
            var endpoint = host.RegisterTestingEndpoint(setup: builder =>
            {
                builder.RegisterCurrentTestsConfiguredPersistenceLayer();
                setup(builder);
                //Hack to get the host to be disposed by the container when the container is disposed.
                builder.Container.Register(Singleton.For<TestingEndpointHostDisposer>().CreatedBy(() => new TestingEndpointHostDisposer(host)).DelegateToParentServiceLocatorWhenCloning());
            });

            return endpoint.ServiceLocator;
        }

        public static IDependencyInjectionContainer Create(IRunMode runMode)
        {
            IDependencyInjectionContainer container = TestEnv.DIContainer.Current switch
            {
                DIContainer.Com => new ComposableDependencyInjectionContainer(runMode),
                DIContainer.Sim => new SimpleInjectorDependencyInjectionContainer(runMode),
                DIContainer.Win => new WindsorDependencyInjectionContainer(runMode),
                _ => throw new ArgumentOutOfRangeException()
            };

            container.Register(Singleton.For<IServiceLocator>().CreatedBy(() => container.CreateServiceLocator()));
            return container;
        }

        class TestingEndpointHostDisposer : IDisposable
        {
            readonly ITestingEndpointHost _host;
            public TestingEndpointHostDisposer(ITestingEndpointHost host) => _host = host;
            public void Dispose() => _host.Dispose();
        }
    }
}