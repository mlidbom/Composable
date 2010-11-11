#region usings

using System;
using NUnit.Framework;

#endregion

namespace Void.DomainEvents.Tests
{
    public class StaticHandlesSomethingHappened : IHandles<SomethingHappend>
    {
        public void Handle(SomethingHappend happening)
        {
            ItHappened(happening.Data);
        }

        public static event Action<string> ItHappened = data => { };
    }

    public class HandlesSomethingHappened : IHandles<SomethingHappend>, IDisposable
    {
        public static int Instances { get; private set; }
        public HandlesSomethingHappened()
        {
            Instances++;
        }

        public void Dispose()
        {
            Instances--;
        }

        public void Handle(SomethingHappend happening)
        {
        }
    }

    public class SomethingHappend : IDomainEvent
    {
        public string Data;
    }

    [TestFixture]
    public class WhenEventIsRaised
    {
        [Test]
        public void SubscribersAreNotified()
        {
            var blah = "";
            StaticHandlesSomethingHappened.ItHappened += data => blah = data;
            DomainEvent.Raise(new SomethingHappend {Data = "Hi"});
            Assert.That(blah, Is.EqualTo("Hi"));
        }

        [Test]
        public void HandlersAreNotReused()
        {
            var initialInstances = HandlesSomethingHappened.Instances;
            DomainEvent.Raise(new SomethingHappend(){Data = "Urg"});
            Assert.That(HandlesSomethingHappened.Instances, Is.GreaterThanOrEqualTo(++initialInstances));

            DomainEvent.Raise(new SomethingHappend() { Data = "Urg" });
            Assert.That(HandlesSomethingHappened.Instances, Is.GreaterThanOrEqualTo(++initialInstances));
        }
    }
}