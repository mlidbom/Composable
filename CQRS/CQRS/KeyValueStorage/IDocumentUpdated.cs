using System;

namespace Composable.KeyValueStorage
{
    public interface IDocumentUpdated
    {
        Type DocumentType { get; }
        string Key { get; }
    }
}