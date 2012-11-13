using Composable.KeyValueStorage.SqlServer;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;

namespace Composable.KeyValueStorage
{
    public class InMemoryDocumentDb : IDocumentDb
    {
        private readonly IObjectStore _store = new InMemoryObjectStore();
        protected InMemoryDocumentDbConfig Config { get; private set; }

        public IObjectStore CreateStore()
        {
            return _store;
        }

        public InMemoryDocumentDb(InMemoryDocumentDbConfig config = null)
        {
            if (config == null)
            {
                Config = InMemoryDocumentDbConfig.Default;
            }
            else
            {
                Config = config;
            }
        }

        public IDocumentDbSession OpenSession(ISingleContextUseGuard guard)
        {
            return new DocumentDbSession(this, guard, this.Config);
        }
    }
}