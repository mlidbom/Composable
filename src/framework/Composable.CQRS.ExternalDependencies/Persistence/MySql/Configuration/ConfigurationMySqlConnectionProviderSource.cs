using Composable.Persistence.MySql.SystemExtensions;
using Composable.System.Configuration;

namespace Composable.Persistence.MySql.Configuration
{
    ///<summary>Supplies connection strings from the application configuration file.</summary>
    class ConfigurationMyMySqlConnectionProviderSource : IMyMySqlConnectionProviderSource
    {
        readonly IConfigurationParameterProvider _configurationParameterProvider;
        public ConfigurationMyMySqlConnectionProviderSource(IConfigurationParameterProvider configurationParameterProvider) => _configurationParameterProvider = configurationParameterProvider;
        ///<summary>Returns the connection string with the given name.</summary>
        public IMySqlConnectionProvider GetConnectionProvider(string connectionStringName) => new MySqlConnectionProvider(_configurationParameterProvider.GetString(connectionStringName));
    }
}
