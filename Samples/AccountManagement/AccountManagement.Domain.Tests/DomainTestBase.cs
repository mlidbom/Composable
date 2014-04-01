using System;
using AccountManagement.TestHelpers;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests
{
    [TestFixture]
    public abstract class DomainTestBase
    {
        protected WindsorContainer Container { get; private set; }
        private IDisposable Scope { get; set; }
        protected MessageSpy MessageSpy { get { return Container.Resolve<MessageSpy>(); } }

        [SetUp]
        public void SetupContainerAndBeginScope()
        {
            Container = DomainTestWiringHelper.SetupContainerForTesting();
            Scope = Container.BeginScope();
        }

        [TearDown]
        public void DisposeScopeAndContainer()
        {
            Scope.Dispose();
            Container.Dispose();
        }
    }
}
