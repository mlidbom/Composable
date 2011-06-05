#region usings

using System.Transactions;

#endregion

namespace Composable.KeyValueStorage.SqlServer
{
    public class SqlServerDocumentDb : IDocumentDb
    {
        public string ConnectionString { get; private set; }
        public SqlServerDocumentDbConfig Config { get; private set; }

        public IObjectStore CreateStore()
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

        public IDocumentDbSession OpenSession()
        {
            return new DocumentDbSession(this, Config);
        }

        public static void ResetDB(string connectionString)
        {
            var me = new SqlServerDocumentDb(connectionString);
            using (var session = new SqlServerObjectStore(me))
            {
                session.PurgeDb();
            }
        }
    }
}