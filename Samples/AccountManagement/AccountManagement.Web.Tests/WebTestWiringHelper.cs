using Castle.Windsor;
using Composable.CQRS.Windsor.Testing;

namespace AccountManagement.Web.Tests
{
    public class WebTestWiringHelper
    {
        public static WindsorContainer CreateContainer()
        {
            var container = new WindsorContainer();
            container.ConfigureWiringForTestsCallBeforeAllOtherWiring();
            ApplicationBootstrapper.ConfigureContainerForTests(container);
            container.ConfigureWiringForTestsCallAfterAllOtherWiring();
            return container;
        }
    }
}
