using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Composable.System.Configuration
{
    ///<summary>Fetches configuration variables from the application configuration file.</summary>
    class AppSettingsJsonConfigurationParameterProvider : IConfigurationParameterProvider
    {
        public static readonly IConfigurationParameterProvider Instance = new AppSettingsJsonConfigurationParameterProvider();
        static readonly IConfigurationSection AppSettingsSection;

        static AppSettingsJsonConfigurationParameterProvider()
        {
            IConfiguration config = new ConfigurationBuilder()
                                   .SetBasePath(Directory.GetCurrentDirectory())
                                   .AddJsonFile("appsettings.json", false, true)
                                   .AddJsonFile("appsettings-testing.json", true, true)
                                   .Build();

            AppSettingsSection = config.GetSection("appSettings");
        }

        public string GetString(string parameterName, string? valueIfMissing = null)
        {
            var parameter = AppSettingsSection[parameterName];
            if(parameter != null) return parameter;
            if(valueIfMissing != null)
            {
                return valueIfMissing;
            }
            throw new Exception($"ApplicationSettings Parameter {parameterName} does not exists");
        }
    }
}