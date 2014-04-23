using System.Configuration;

namespace Composable.System.Configuration
{
    ///<summary>Fetches configuration variables from the application configuration file.</summary>
    public class AppConfigConfigurationParameterProvider : IConfigurationParameterProvider
    {
        ///<summary>Gets a string configuration value.</summary>
        public string GetString(string parameterName)
        {
            var parameter = ConfigurationManager.AppSettings[parameterName];
            if (parameter==null)
            {
                throw new ConfigurationErrorsException(string.Format("ApplicationSettings Parameter {0} does not exists",parameterName));
            }
            return parameter;
        }
    }
}
