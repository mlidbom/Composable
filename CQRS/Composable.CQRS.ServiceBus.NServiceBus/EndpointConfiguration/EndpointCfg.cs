#region usings

using System;
using System.Configuration;
using Composable.System;

#endregion

namespace Composable.CQRS.ServiceBus.NServiceBus.EndpointConfiguration
{
    public static class EndpointCfg
    {
        public const string EnvironmentNameConfigParamName = "EnvironmentName";
        public const string EnvironmentNameMessageHeaderName = EnvironmentNameConfigParamName;        

        private static void Init()
        {
            if (_environmentName.IsNullOrWhiteSpace())
            {
                _environmentName = ConfigurationManager.AppSettings.Get(EnvironmentNameConfigParamName);
                if (_environmentName.IsNullOrWhiteSpace())
                {
                    throw new Exception("The configuration parameter {0} does not have a valid value".FormatWith(EnvironmentNameConfigParamName));
                }
            }
        }

        private static string _environmentName;
        public static string EnvironmentName
        {
            get
            {
                Init();
                return _environmentName;
            }
        }
    }
}