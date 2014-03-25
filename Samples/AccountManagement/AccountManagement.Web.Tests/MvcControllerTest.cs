using System;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using NUnit.Framework;

namespace AccountManagement.Web.Tests
{
    [TestFixture]
    public abstract class MvcControllerTest 
    {
        protected WindsorContainer Container;        
        private IDisposable _scope;

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