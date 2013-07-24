using Composable.KeyValueStorage.SqlServer;
using Composable.SystemExtensions.Threading;

namespace Composable.KeyValueStorage
{
    public interface IDocumentDb
    {
        IObservableObjectStore CreateStore();
        IDocumentDbSession OpenSession(ISingleContextUseGuard guard);
    }
}