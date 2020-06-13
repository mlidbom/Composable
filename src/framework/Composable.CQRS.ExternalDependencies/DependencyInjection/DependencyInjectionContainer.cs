using System;
using Composable.Messaging.Buses;
using JetBrains.Annotations;

namespace Composable.DependencyInjection
{
    public static class DependencyInjectionContainer
    {
        public static IServiceLocator CreateServiceLocatorForTesting() => CreateServiceLocatorForTesting(_ => {}, TestingMode.DatabasePool);

        public static IServiceLocator CreateServiceLocatorForTesting(TestingMode mode) => CreateServiceLocatorForTesting(_ => {}, mode);

        public static IServiceLocator CreateServiceLocatorForTesting([InstantHandle] Action<IDependencyInjectionContainer> setup) => CreateServiceLocatorForTesting(setup, TestingMode.DatabasePool);

        public static IServiceLocator CreateServiceLocatorForTesting([InstantHandle]Action<IDependencyInjectionContainer> setup, TestingMode mode)
        {
#pragma warning disable IDE0067 // Dispose objects before losing scope
            var host = EndpointHost.Testing.Create(Create, mode);
#pragma warning restore IDE0067 // Dispose objects before losing scope
            var endpoint = host.RegisterTestingEndpoint(setup: builder =>
            {
                setup(builder.Container);
                //Hack to get the host to be disposed by the container when the container is disposed.
                builder.Container.Register(Singleton.For<TestingEndpointHostDisposer>().CreatedBy(() => new TestingEndpointHostDisposer(host)).DelegateToParentServiceLocatorWhenCloning());
            });

            return endpoint.ServiceLocator;
        }

        public static IDependencyInjectionContainer Create() => Create(RunMode.Production);
        public static IDependencyInjectionContainer Create(IRunMode runMode)
        {
            //IDependencyInjectionContainer container = new SimpleInjectorDependencyInjectionContainer(runMode ?? DependencyInjection.RunMode.Production);
            //IDependencyInjectionContainer container = new WindsorDependencyInjectionContainer(runMode ?? DependencyInjection.RunMode.Production);
            IDependencyInjectionContainer container = new ComposableDependencyInjectionContainer(runMode);
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