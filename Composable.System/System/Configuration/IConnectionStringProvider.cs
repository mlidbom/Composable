using System.Configuration;

namespace Composable.System.Configuration
{
    ///<summary>Fetches connections strings from a configuration source such as the application configuration file.</summary>
    public interface IConnectionStringProvider
    {
        ///<summary>Returns the connection string with the given name.</summary>
        ConnectionStringSettings GetConnectionString(string parameterName);
    }
}