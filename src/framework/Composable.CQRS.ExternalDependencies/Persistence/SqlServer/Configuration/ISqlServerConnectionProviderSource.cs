using Composable.Persistence.SqlServer.SystemExtensions;

namespace Composable.Persistence.SqlServer.Configuration
{
    ///<summary>Fetches connections strings from a configuration source such as the application configuration file.</summary>
    interface ISqlServerConnectionProviderSource
    {
        ///<summary>Returns the connection string with the given name.</summary>
        ISqlConnectionProvider GetConnectionProvider(string connectionStringName);
    }
}