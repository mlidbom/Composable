using System;
using Composable.System.Data.SqlClient;

namespace Composable.System.Configuration
{
    ///<summary>Fetches connections strings from a configuration source such as the application configuration file.</summary>
    interface IConnectionStringProvider
    {
        ///<summary>Returns the connection string with the given name.</summary>
        ISqlConnectionProvider GetConnectionProvider(string parameterName);
    }
}