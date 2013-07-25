#region usings

using System.Transactions;
using Composable.SystemExtensions.Threading;

#endregion

namespace Composable.KeyValueStorage.SqlServer
{
    public class SqlServerDocumentDb : IDocumentDb
    {
        public string ConnectionString { get; private set; }
        public SqlServerDocumentDbConfig Config { get; private set; }

        public IObservableObjectStore CreateStore()
        {
            return new SqlServerObjectStore(this);
        }

        public SqlServerDocumentDb(string connectionString, SqlServerDocumentDbConfig config = null)
        {
            if(config == null)
            {
                config = SqlServerDocumentDbConfig.Default;
            }
            ConnectionString = connectionString;
            Config = config;
        }

        public IDocumentDbSession OpenSession(ISingleContextUseGuard guard)
        {
            return new DocumentDbSession(this, guard, Config);
        }

        public static void ResetDB(string connectionString)
        {
            SqlServerObjectStore.PurgeDb(connectionString);
        }
    }
}