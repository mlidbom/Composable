using System;

namespace Composable.KeyValueStorage
{
    public class DocumentUpdated : IDocumentUpdated
    {
        public object Document { get; private set; }
        public object Key { get; private set; }

        public DocumentUpdated(object document, string key)
        {
            Document = document;
            Key = key;
        }
    }
}