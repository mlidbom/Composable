using Composable.Persistence.MySql.SystemExtensions;

namespace Composable.Persistence.MySql.Configuration
{
    //urgent: Remove this whole interface. With current wiring it just adds confusion and complexity
    ///<summary>Fetches connections strings from a configuration source such as the application configuration file.</summary>
    interface IMySqlConnectionProviderSource
    {
        ///<summary>Returns the connection string with the given name.</summary>
        IMySqlConnectionProvider GetConnectionProvider(string connectionStringName);
    }
}