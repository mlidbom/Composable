using System.Configuration;
using Composable.System.Data.SqlClient;

namespace Composable.System.Configuration
{
    ///<summary>Supplies connection strings from the application configuration file.</summary>
    class ConfigurationSqlConnectionProviderSource : ISqlConnectionProviderSource
    {
        readonly IConfigurationParameterProvider _configurationParameterProvider;
        public ConfigurationSqlConnectionProviderSource(IConfigurationParameterProvider configurationParameterProvider) => _configurationParameterProvider = configurationParameterProvider;
        ///<summary>Returns the connection string with the given name.</summary>
        public ISqlConnectionProvider GetConnectionProvider(string connectionStringName) => new SqlServerConnectionProvider(_configurationParameterProvider.GetString(connectionStringName));
    }
}
