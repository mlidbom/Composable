#region usings

using System;
using System.Threading;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using CommonServiceLocator.WindsorAdapter;
using Composable.System;
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
            container.Register(AllTypes.FromThisAssembly().BasedOn(typeof(IHandles<>)).Configure(cfg => cfg.LifeStyle.Transient));
            DomainEvent.Init(new WindsorServiceLocator(container));
        }

        [TearDown]
        public void TearDown()
        {
            DomainEvent.ResetOnlyUseFromTests();
        }

        public class SomethingHappend : IDomainEvent
        {
        }

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
            var called = false;
            using(DomainEvent.RegisterShortTermSynchronousListener<IDomainEvent>(i => { called = true; }))
            {
                DomainEvent.Raise(new SomethingHappend());
            }
            Assert.That(called, Is.True);
        }

        [Test]
        public void ManuallyRegisteredListenersAreNotCalledWhenEventRaisedOnOtherThread()
        {
            var called = false;
            using (DomainEvent.RegisterShortTermSynchronousListener<IDomainEvent>(i => { called = true; }))
            {
                var done = new ManualResetEvent(false);
                using(var timer = new Timer((o) =>
                                                {
                                                    DomainEvent.Raise(new SomethingHappend());
                                                    done.Set();
                                                }, null, 1, -1))
                {
                    done.WaitOne(2.Seconds());
                }
            }
            Assert.That(called, Is.False);
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