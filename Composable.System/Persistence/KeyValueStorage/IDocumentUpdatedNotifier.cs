using System;

namespace Composable.Persistence.KeyValueStorage
{
    public interface IDocumentUpdatedNotifier
    {
        IObservable<IDocumentUpdated> DocumentUpdated { get; }
    }
}