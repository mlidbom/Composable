using System;
using Composable.KeyValueStorage.SqlServer;

namespace Composable.KeyValueStorage
{
    public interface IObservableObjectStore : IObservable<IDocumentUpdated>, IObjectStore
    {
        
    }
}