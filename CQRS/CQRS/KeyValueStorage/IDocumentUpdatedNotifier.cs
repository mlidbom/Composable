using System;

namespace Composable.KeyValueStorage
{
    public interface IDocumentUpdatedNotifier : IObservable<IDocumentUpdated>{}
}