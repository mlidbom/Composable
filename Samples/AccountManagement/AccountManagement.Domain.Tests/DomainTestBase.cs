using AccountManagement.TestHelpers;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using Composable.System;
using Composable.Windsor.Testing;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests
{
    [TestFixture] public abstract class DomainTestBase
    {
        protected IWindsorContainer Container { get; private set; }
        protected MessageSpy MessageSpy { get { return Container.Resolve<MessageSpy>(); } }

        StrictAggregateDisposable _managedResources;

        [SetUp] public void SetupContainerAndBeginScope()
        {
            Container = DomainTestWiringHelper.SetupContainerForTesting();

            _managedResources = StrictAggregateDisposable.Create(Container.BeginScope(), Container);
        }

        [TearDown] public void DisposeScopeAndContainer() { _managedResources.Dispose(); }
    }
}
