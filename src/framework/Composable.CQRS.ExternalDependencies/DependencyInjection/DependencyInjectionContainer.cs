using System;
using Composable.DependencyInjection.SimpleInjectorImplementation;
using Composable.DependencyInjection.Testing;
using Composable.DependencyInjection.Windsor;
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
            var host = EndpointHost.Testing.CreateHost(Create, mode);
            var endpoint = host.RegisterTestingEndpoint(setup: builder =>
            {
                setup(builder.Container);
                builder.Container.Register(Component.For<ITestingEndpointHost>().UsingFactoryMethod(() => host).LifestyleSingleton().DelegateToParentServiceLocatorWhenCloning());
            });

            return endpoint.ServiceLocator;
        }

        public static IDependencyInjectionContainer Create(IRunMode runMode = null)
        {
            //IDependencyInjectionContainer container = new SimpleInjectorDependencyInjectionContainer(runMode ?? DependencyInjection.RunMode.Production);
            //IDependencyInjectionContainer container = new WindsorDependencyInjectionContainer(runMode ?? DependencyInjection.RunMode.Production);
            IDependencyInjectionContainer container = new ComposableDependencyInjectionContainer(runMode);
            container.Register(Component.For<IServiceLocator>()
                                        .UsingFactoryMethod(() => container.CreateServiceLocator())
                                        .LifestyleSingleton());
            return container;
        }
    }
}