using System;
using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.Persistence.SqlServer.Testing.Databases;
using Composable.System.Configuration;

namespace Composable.Persistence.SqlServer.Configuration
{
    class SqlServerServerDatabasePoolSqlServerConnectionProviderSource : ISqlServerConnectionProviderSource, IDisposable
    {
        readonly SqlServerDatabasePool _pool;
        public SqlServerServerDatabasePoolSqlServerConnectionProviderSource(IConfigurationParameterProvider configurationParameterProvider) => _pool = new SqlServerDatabasePool(configurationParameterProvider);

        public ISqlServerConnectionProvider GetConnectionProvider(string connectionStringName) => _pool.ConnectionProviderFor(connectionStringName);
        public void Dispose() => _pool.Dispose();
    }
}