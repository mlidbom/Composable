using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.System.Configuration;

namespace Composable.Persistence.SqlServer.Configuration
{
    ///<summary>Supplies connection strings from the application configuration file.</summary>
    class ConfigurationSqlServerConnectionProviderSource : ISqlServerConnectionProviderSource
    {
        readonly IConfigurationParameterProvider _configurationParameterProvider;
        public ConfigurationSqlServerConnectionProviderSource(IConfigurationParameterProvider configurationParameterProvider) => _configurationParameterProvider = configurationParameterProvider;
        ///<summary>Returns the connection string with the given name.</summary>
        public ISqlConnectionProvider GetConnectionProvider(string connectionStringName) => new SqlServerConnectionProvider(_configurationParameterProvider.GetString(connectionStringName));
    }
}
