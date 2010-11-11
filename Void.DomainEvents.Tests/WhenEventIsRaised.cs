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

    internal class InternalSomethingHappenedHandler : IHandles<SomethingHappend>
    {
        public void Handle(SomethingHappend args)
        {
            ItHappened(args.Data);
        }
        public static event Action<string> ItHappened = data => { };
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
        public void InternalSubscribersAreNotified()
        {
            var blah = "";
            InternalSomethingHappenedHandler.ItHappened += data => blah = data;
            DomainEvent.Raise(new SomethingHappend { Data = "Hi" });
            Assert.That(blah, Is.EqualTo("Hi"));
        }

        [Test]
        public void HandlersAreNotReused()
        {
            DomainEvent.Raise(new SomethingHappend(){Data = "Urg"});
            Assert.That(HandlesSomethingHappened.Instances, Is.EqualTo(1));

            DomainEvent.Raise(new SomethingHappend() { Data = "Urg" });
            Assert.That(HandlesSomethingHappened.Instances, Is.EqualTo(2));
        }
    }
}