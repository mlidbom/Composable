
namespace Composable.Persistence.KeyValueStorage
{
    class DocumentUpdated<TDocument> : IDocumentUpdated<TDocument>
    {
        public TDocument Document { get; private set; }
        public string Key { get; private set; }

        internal DocumentUpdated(string key, TDocument document)
        {
            Document = document;
            Key = key;
        }
    }

    class DocumentUpdated : DocumentUpdated<object>, IDocumentUpdated
    {
        public DocumentUpdated(string key, object document) : base(key, document) { }
    }
}