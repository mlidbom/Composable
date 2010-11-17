using System;
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
        [Test]
        public void DomainEventClassThrowsException()
        {
            Assert.Throws<TypeInitializationException>(() => DomainEvent.Raise(new SomethingHappend()));
        }
    }
}