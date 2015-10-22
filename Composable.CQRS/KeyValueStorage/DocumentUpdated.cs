
namespace Composable.KeyValueStorage
{
    public class DocumentUpdated<TDocument> : IDocumentUpdated<TDocument>
    {
        public TDocument Document { get; private set; }
        public string Key { get; private set; }

        public DocumentUpdated(string key, TDocument document)
        {
            Document = document;
            Key = key;
        }
    }

    public class DocumentUpdated : DocumentUpdated<object>, IDocumentUpdated
    {
        public DocumentUpdated(string key, object document) : base(key, document) { }
    }    
}