using System.Configuration;

namespace Composable.System.Configuration
{
    public class ConnectionStringConfigurationParameterProvider : IConnectionStringProvider
    {
        public ConnectionStringSettings GetConnectionString(string parameterName)
        {
            var parameter = ConfigurationManager.ConnectionStrings[parameterName];
            if (parameter==null)
            {
                throw new ConfigurationErrorsException(string.Format("ConnectionString with name {0} does not exists",parameterName));
            }
            return parameter;
        }
    }
}
