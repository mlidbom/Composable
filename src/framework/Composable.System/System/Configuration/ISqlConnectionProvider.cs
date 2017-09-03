using Composable.System.Data.SqlClient;

namespace Composable.System.Configuration
{
    ///<summary>Fetches connections strings from a configuration source such as the application configuration file.</summary>
    interface ISqlConnectionProvider
    {
        ///<summary>Returns the connection string with the given name.</summary>
        ISqlConnection GetConnectionProvider(string parameterName);
    }
}