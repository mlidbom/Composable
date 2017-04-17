using Composable.Persistence.EventStore;
using NUnit.Framework;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations {
    [TestFixture]
    public class EventStreamMutatorTests_InMemory : EventStreamMutatorTests
    {
        public EventStreamMutatorTests_InMemory() : base(typeof(InMemoryEventStore)) { }
    }
}