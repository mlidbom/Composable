#region usings

using System;
using System.Threading;
using Composable.System;
using NUnit.Framework;

#endregion

namespace Composable.DomainEvents.Tests
{
    [TestFixture]
    public class WhenEventIsRaised
    {
        public class SomethingHappend : IDomainEvent
        {
        }


        [Test]
        public void ManuallyRegisteredListenersAreCalled()
        {
            var called = false;
#pragma warning disable 612,618
            using(DomainEvent.RegisterShortTermSynchronousListener<IDomainEvent>(i => { called = true; }))
#pragma warning restore 612,618
            {
                DomainEvent.Raise(new SomethingHappend());
            }
            Assert.That(called, Is.True);
        }

        [Test]
        public void ManuallyRegisteredListenersAreNotCalledWhenEventRaisedOnOtherThread()
        {
            var called = false;
#pragma warning disable 612,618
            using (DomainEvent.RegisterShortTermSynchronousListener<IDomainEvent>(i => { called = true; }))
#pragma warning restore 612,618
            {
                var done = new ManualResetEventSlim();
                using(var timer = new Timer((o) =>
                                                {
                                                    DomainEvent.Raise(new SomethingHappend());
                                                    done.Set();
                                                }, null, 1, -1))
                {
                    done.Wait(2.Seconds());
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