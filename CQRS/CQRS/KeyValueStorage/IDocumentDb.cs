using Composable.KeyValueStorage.SqlServer;

namespace Composable.KeyValueStorage
{
    public interface IDocumentDb
    {
        IObjectStore CreateStore();
        IDocumentDbSession OpenSession();
    }
}