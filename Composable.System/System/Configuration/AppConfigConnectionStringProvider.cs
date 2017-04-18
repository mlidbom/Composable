using System;
using System.Configuration;

namespace Composable.System.Configuration
{
    ///<summary>Supplies connection strings from the application configuration file.</summary>
    class AppConfigConnectionStringProvider : IConnectionStringProvider
    {
        ///<summary>Returns the connection string with the given name.</summary>
        public Lazy<string> GetConnectionString(string parameterName)
        {
            return new Lazy<string>(() =>
                                    {
                                        var parameter = ConfigurationManager.ConnectionStrings[parameterName];
                                        if(parameter == null)
                                        {
                                            throw new ConfigurationErrorsException($"ConnectionString with name {parameterName} does not exists");
                                        }
                                        return parameter.ConnectionString;
                                    });
        }
    }
}
