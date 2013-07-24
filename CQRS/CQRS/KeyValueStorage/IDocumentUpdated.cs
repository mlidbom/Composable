using System;

namespace Composable.KeyValueStorage
{
    public interface IDocumentUpdated
    {
        object Document { get; }
        object Key { get; }
    }
}