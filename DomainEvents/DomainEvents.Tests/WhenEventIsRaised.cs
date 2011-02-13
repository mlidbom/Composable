#region usings

using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using CommonServiceLocator.WindsorAdapter;
using NUnit.Framework;

#endregion

namespace Composable.DomainEvents.Tests
{
    [TestFixture]
    public class WhenEventIsRaised
    {

        [SetUp]
        public void Setup()
        {
            var container = new WindsorContainer();
            container.Register(AllTypes.FromThisAssembly().BasedOn(typeof (IHandles<>)).Configure(cfg => cfg.LifeStyle.Transient));
            DomainEvent.ReInitOnlyUseFromTests(new WindsorServiceLocator(container));
        }

        public class SomethingHappend : IDomainEvent
        { }

        [Test]
        public void SubscribersAreNotified()
        {
            var calls = 0;
            HandlesSomethingHappened.IWasNotified += () => calls++;
            DomainEvent.Raise(new SomethingHappend());
            Assert.That(calls, Is.EqualTo(1));
        }
      

        [Test]
        public void ManuallyRegisteredListenersAreCalled()
        {
            Assert.Inconclusive();
        }

        public class HandlesSomethingHappened : IHandles<SomethingHappend>
        {
            public void Handle(SomethingHappend happening)
            {
                IWasNotified();
            }

            public static event Action IWasNotified = () => { };
        }
    }
}