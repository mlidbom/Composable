using System;
using Composable.System.Data.SqlClient;
using Composable.Testing.Databases;

namespace Composable.System.Configuration
{
    class SqlServerDatabasePoolSqlConnectionProviderSource : ISqlConnectionProviderSource, IDisposable
    {
        readonly SqlServerDatabasePool _pool;
        public SqlServerDatabasePoolSqlConnectionProviderSource() => _pool = new SqlServerDatabasePool();
        public Data.SqlClient.ISqlConnectionProvider GetConnectionProvider(string connectionStringName) => _pool.ConnectionProviderFor(connectionStringName);
        public void Dispose() => _pool.Dispose();
    }
}