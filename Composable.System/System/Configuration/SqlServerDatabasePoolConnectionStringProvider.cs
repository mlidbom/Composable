using System;
using System.Configuration;
using Composable.Testing;

namespace Composable.System.Configuration
{
    class SqlServerDatabasePoolConnectionStringProvider : IConnectionStringProvider, IDisposable
    {
        readonly SqlServerDatabasePool _pool;
        public SqlServerDatabasePoolConnectionStringProvider(string masterConnectionString) => _pool = new SqlServerDatabasePool(masterConnectionString);
        public ConnectionStringSettings GetConnectionString(string parameterName) => new ConnectionStringSettings(parameterName, _pool.ConnectionStringFor(parameterName));
        public void Dispose() => _pool.Dispose();
    }
}