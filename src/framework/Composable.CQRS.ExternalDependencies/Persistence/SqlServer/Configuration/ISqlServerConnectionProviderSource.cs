using Composable.Persistence.SqlServer.SystemExtensions;

namespace Composable.Persistence.SqlServer.Configuration
{
    //urgent: Remove this whole interface. With current wiring it just adds confusion and complexity
    ///<summary>Fetches connections strings from a configuration source such as the application configuration file.</summary>
    interface ISqlServerConnectionProviderSource
    {
        ///<summary>Returns the connection string with the given name.</summary>
        ISqlServerConnectionProvider GetConnectionProvider(string connectionStringName);
    }
}