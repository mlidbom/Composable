using System;
using Composable.Testing.Databases;
using Composable.Testing.System.Data.SqlClient;

namespace Composable.Testing.System.Configuration
{
    class SqlServerDatabasePoolSqlConnectionProvider : ISqlConnectionProvider, IDisposable
    {
        readonly SqlServerDatabasePool _pool;
        public SqlServerDatabasePoolSqlConnectionProvider(string masterConnectionString) => _pool = new SqlServerDatabasePool(masterConnectionString);
        public ISqlConnection GetConnectionProvider(string parameterName) => _pool.ConnectionProviderFor(parameterName);
        public void Dispose() => _pool.Dispose();
    }
}