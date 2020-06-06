using Composable.Messaging;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.EventStore;
// ReSharper disable MemberCanBeMadeStatic.Global we want composable fluent APIs. No statics please.

namespace Composable
{
    public class ComposableApi
    {
        public EventStoreApi EventStore => new EventStoreApi();
        public DocumentDbApi DocumentDb => new DocumentDbApi();
    }
}
