using System;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using NUnit.Framework;

namespace AccountManagement.UI.Web.Tests
{
    [TestFixture]
    public abstract class ControllerTest
    {
        protected WindsorContainer Container;
        IDisposable _scope;

        [SetUp]
        public void SetupContainer()
        {
            Container = WebTestWiringHelper.CreateContainer();
            _scope = Container.BeginScope();
        }

        [TearDown]
        public void CleanUpScope()
        {
            _scope.Dispose();
            Container.Dispose();
        }
    }
}
