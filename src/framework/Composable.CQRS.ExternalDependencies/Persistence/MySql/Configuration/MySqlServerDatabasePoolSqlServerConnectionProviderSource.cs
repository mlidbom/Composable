using System;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.Persistence.MySql.Testing.Databases;
using Composable.System.Configuration;

namespace Composable.Persistence.MySql.Configuration
{
    class MySqlServerDatabasePoolMySqlConnectionProviderSource : IMySqlConnectionProviderSource, IDisposable
    {
        readonly MySqlDatabasePool _pool;
        public MySqlServerDatabasePoolMySqlConnectionProviderSource(IConfigurationParameterProvider configurationParameterProvider) => _pool = new MySqlDatabasePool(configurationParameterProvider);

        public IMySqlConnectionProvider GetConnectionProvider(string connectionStringName) => _pool.ConnectionProviderFor(connectionStringName);
        public void Dispose() => _pool.Dispose();
    }
}