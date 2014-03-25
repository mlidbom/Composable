using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.Windsor.Testing;

namespace AccountManagement.Web.Tests
{
    public static class WebTestWiringHelper
    {
        public static WindsorContainer CreateContainerWithAuthenticationContext()
        {
            var container = CreateContainer();
            container.Register(
                Component.For<IAuthenticationContext, TestAuthenticationContext>()
                    .Instance(new TestAuthenticationContext())
                );
            return container;
        }

        public static WindsorContainer CreateContainer()
        {
            var container = new WindsorContainer();
            CommonWiring(container);
            return container;
        }

        private static void CommonWiring(IWindsorContainer container)
        {
            container.ConfigureWiringForTestsCallBeforeAllOtherWiring();

            ApplicationBootstrapper.ConfigureContainerForTests(container);

            container.ConfigureWiringForTestsCallAfterAllOtherWiring();
        }
    }

    public class TestAuthenticationContext : IAuthenticationContext
    {
        public Guid AccountId { get; set; }
    }
}
