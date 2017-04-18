using System;
using Composable.System.Data.SqlClient;
using Composable.Testing;

namespace Composable.System.Configuration
{
    class SqlServerDatabasePoolConnectionStringProvider : IConnectionStringProvider, IDisposable
    {
        readonly SqlServerDatabasePool _pool;
        public SqlServerDatabasePoolConnectionStringProvider(string masterConnectionString) => _pool = new SqlServerDatabasePool(masterConnectionString);
        public ISqlConnectionProvider GetConnectionProvider(string parameterName) => _pool.ConnectionProviderFor(parameterName);
        public void Dispose() => _pool.Dispose();
    }
}