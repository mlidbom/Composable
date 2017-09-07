using System.Configuration;
using Composable.Testing.System.Data.SqlClient;

namespace Composable.Testing.System.Configuration
{
    ///<summary>Supplies connection strings from the application configuration file.</summary>
    class AppConfigSqlConnectionProvider : ISqlConnectionProvider
    {
        ///<summary>Returns the connection string with the given name.</summary>
        public ISqlConnection GetConnectionProvider(string parameterName)
        {
            var parameter = ConfigurationManager.ConnectionStrings[parameterName];
            if (parameter == null)
            {
                throw new ConfigurationErrorsException($"ConnectionString with name {parameterName} does not exists");
            }
            return new SqlServerConnection(parameter.ConnectionString);
        }
    }
}
