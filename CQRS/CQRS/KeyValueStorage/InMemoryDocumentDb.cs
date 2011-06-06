using Composable.KeyValueStorage.SqlServer;
using Composable.System.Linq;

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

        public IDocumentDbSession OpenSession()
        {
            return new DocumentDbSession(this, this.Config);
        }
    }
}