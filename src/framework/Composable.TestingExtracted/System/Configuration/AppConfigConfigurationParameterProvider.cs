using System.Configuration;

namespace Composable.Testing.System.Configuration
{
    ///<summary>Fetches configuration variables from the application configuration file.</summary>
    class AppConfigConfigurationParameterProvider : IConfigurationParameterProvider
    {
        public static readonly IConfigurationParameterProvider Instance = new AppConfigConfigurationParameterProvider();

        public string GetString(string parameterName, string valueIfMissing = null)
        {
            var parameter = ConfigurationManager.AppSettings[parameterName];
            if(parameter != null) return parameter;
            if(valueIfMissing != null)
            {
                return valueIfMissing;
            }
            throw new ConfigurationErrorsException($"ApplicationSettings Parameter {parameterName} does not exists");
        }
    }
}
