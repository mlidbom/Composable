using System.Collections.Generic;
using System.Linq;
using Composable.System.Configuration;

namespace Composable.Messaging.Buses.Implementation
{
    class AppConfigEndpointRegistry : IEndpointRegistry
    {
        readonly IConfigurationParameterProvider _settingsProvider;
        public AppConfigEndpointRegistry(IConfigurationParameterProvider settingsProvider) => _settingsProvider = settingsProvider;

        public IEnumerable<EndPointAddress> ServerEndpoints
        {
            get
            {
                var configurationValue = _settingsProvider.GetString("ServerEndpoints");
                var addresses = configurationValue.Split(';')
                                                  .Select(stringValue => stringValue.Trim())
                                                  .Where(stringValue => !string.IsNullOrEmpty(stringValue))
                                                  .Select(stringValue => new EndPointAddress(stringValue)).ToList();
                return addresses;
            }
        }
    }
}
