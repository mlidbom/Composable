#region usings

using System;
using NUnit.Framework;

#endregion

namespace Void.DomainEvents.Tests
{
    public class SomethingHappend : IDomainEvent
    {}

    [TestFixture]
    public class WhenEventIsRaised
    {
        [Test]
        public void SubscribersAreNotified()
        {
            var calls = 0;
            HandlesSomethingHappened.ItHappened += () => calls++;
            DomainEvent.Raise(new SomethingHappend());
            Assert.That(calls, Is.EqualTo(1));
        }
      

        [Test]
        public void ManuallyRegisteredListenersAreCalled()
        {
            
        }

        public class HandlesSomethingHappened : IHandles<SomethingHappend>
        {
            public void Handle(SomethingHappend happening)
            {
                ItHappened();
            }

            public static event Action ItHappened = () => { };
        }
    }
}