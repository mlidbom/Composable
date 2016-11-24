using System;
using Castle.Windsor;
using Composable.Windsor.Testing;

namespace AccountManagement.UI.Web.Tests
{
    public static class WebTestWiringHelper
    {
        public static WindsorContainer CreateContainer()
        {
            var container = new WindsorContainer();
            ApplicationBootstrapper.ConfigureContainerForTests(container);
            return container;
        }
    }

    public class TestAuthenticationContext : IAuthenticationContext
    {
        public Guid AccountId { get; set; }
    }
}
