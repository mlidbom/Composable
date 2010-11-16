#region usings

using System;
using NUnit.Framework;

#endregion

namespace Void.DomainEvents.Tests
{
    [TestFixture]
    public class WhenEventIsRaised
    {
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