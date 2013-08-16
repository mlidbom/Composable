using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.Testing;
using Composable.SystemExtensions.Threading;
using NUnit.Framework;

namespace CQRS.Tests.CQRS.EventSourcing
{
    [TestFixture]
    class InMemoryEventStoreSessionTests : EventStoreSessionTests
    {
        protected override IEventStore CreateStore()
        {
            return new InMemoryEventStore(new SingleThreadUseGuard());
        }
    }
}