using System;
using Castle.Windsor;
using CommonServiceLocator.WindsorAdapter;
using NUnit.Framework;

namespace Composable.DomainEvents.TestInternalImplementations
{
    public class SomethingHappend : IDomainEvent
    {
    }

    internal class InternalSomethingHappenedHandler : IHandles<SomethingHappend>
    {
        public void Handle(SomethingHappend args)
        {
        }
    }

    [TestFixture]
    public class WhenInternalIHandlesImplementationExists
    {
        [SetUp]
        public void Setup()
        {
            DomainEvent.Init(new WindsorServiceLocator(new WindsorContainer()));
        }

        [Test]
        public void DomainEventClassThrowsException()
        {
            Assert.Throws<TypeInitializationException>(() => DomainEvent.Raise(new SomethingHappend()));
        }
    }
}