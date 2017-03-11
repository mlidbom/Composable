using System;
using System.Reactive.Linq;
using Composable.Persistence.KeyValueStorage;

namespace Composable.CQRS.Tests
{
    public static class DocumentUpdatedObservableExtensions
    {
        public static IObservable<IDocumentUpdated<TDocument>> WithDocumentType<TDocument>(this IObservable<IDocumentUpdated> me)
        {
            return me.Where(documentUpdated => documentUpdated.Document is TDocument)
                .Select(documentUpdated => new DocumentUpdated<TDocument>(documentUpdated.Key, (TDocument)documentUpdated.Document));
        }

        public static IObservable<TDocument> DocumentsOfType<TDocument>(this IObservable<IDocumentUpdated> me)
        {
            return me.Select(it => it.Document).OfType<TDocument>();
        }
    }
}