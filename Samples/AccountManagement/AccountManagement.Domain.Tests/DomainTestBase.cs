using AccountManagement.TestHelpers;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using Composable.System;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests
{
  using Composable.Messaging.Buses;

  [TestFixture] public abstract class DomainTestBase
    {
        protected IWindsorContainer Container { get; private set; }
        protected IMessageSpy MessageSpy { get { return Container.Resolve<IMessageSpy>(); } }

        StrictAggregateDisposable _managedResources;

        [SetUp] public void SetupContainerAndBeginScope()
        {
            Container = DomainTestWiringHelper.SetupContainerForTesting();

            _managedResources = StrictAggregateDisposable.Create(Container.BeginScope(), Container);
        }

        [TearDown] public void DisposeScopeAndContainer() { _managedResources.Dispose(); }
    }
}
