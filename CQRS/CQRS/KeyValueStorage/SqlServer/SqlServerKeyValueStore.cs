#region usings

using System.Transactions;

#endregion

namespace Composable.KeyValueStorage.SqlServer
{
    public class SqlServerKeyValueStore : IKeyValueStore
    {
        public string ConnectionString { get; private set; }
        public SqlServerKeyValueStoreConfig Config { get; private set; }

        public IObjectStore CreateStore()
        {
            return new SqlServerObjectStore(this);
        }

        public SqlServerKeyValueStore(string connectionString, SqlServerKeyValueStoreConfig config = null)
        {
            if(config == null)
            {
                config = SqlServerKeyValueStoreConfig.Default;
            }
            ConnectionString = connectionString;
            Config = config;
        }        

        public IKeyValueStoreSession OpenSession()
        {
            return new KeyValueSession(this, Config);
        }

        public static void ResetDB(string connectionString)
        {
            var me = new SqlServerKeyValueStore(connectionString);
            using (var session = new SqlServerObjectStore(me))
            {
                session.PurgeDb();
            }
        }
    }
}