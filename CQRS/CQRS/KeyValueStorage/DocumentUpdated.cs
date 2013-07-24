using System;

namespace Composable.KeyValueStorage
{
    public class DocumentUpdated : IDocumentUpdated
    {
        public Type DocumentType { get; private set; }
        public string Key { get; private set; }

        public DocumentUpdated(Type documentType, string key)
        {
            DocumentType = documentType;
            Key = key;
        }
    }
}