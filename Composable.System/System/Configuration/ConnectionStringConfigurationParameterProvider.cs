using System.Configuration;

namespace Composable.System.Configuration
{
    ///<summary>Supplies connection strings from the application configuration file.</summary>
    public class ConnectionStringConfigurationParameterProvider : IConnectionStringProvider
    {
        ///<summary>Returns the connection string with the given name.</summary>
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
