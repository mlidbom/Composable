using System;
using Composable.DomainEvents;
using Composable.System;
using NUnit.Framework;

namespace Composable.CQRS.Testing
{
    public static class EventTests
    {
        public static TEvent AssertEventRaised<TEvent>(Action action) where TEvent : IDomainEvent
        {
            TEvent caughtEvent = default(TEvent);
#pragma warning disable 612,618
            using (DomainEvent.RegisterShortTermSynchronousListener<TEvent>(raisedEvent => caughtEvent = raisedEvent))
#pragma warning restore 612,618
            {
                action();
                Assert.That(caughtEvent, Is.Not.Null, "Event of type {0} should have been raised".FormatWith(typeof(TEvent)));
                return caughtEvent;
            }
        }
    }
}