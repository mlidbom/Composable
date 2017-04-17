using Composable.Persistence.DocumentDb;

namespace Composable.Persistence.EventStore.Query.Models.Generators
{
    interface IVersioningDocumentDbReader : IDocumentDbReader
    {
        bool TryGetVersion<TDocument>(object key, out TDocument document, int version);
        TValue GetVersion<TValue>(object key, int version);
    }
}