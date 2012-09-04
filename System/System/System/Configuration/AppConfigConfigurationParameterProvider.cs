using System.Configuration;

namespace Composable.System.Configuration
{
    public class AppConfigConfigurationParameterProvider : IConfigurationParameterProvider
    {
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
