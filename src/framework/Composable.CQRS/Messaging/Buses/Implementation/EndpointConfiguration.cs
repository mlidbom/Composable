using Composable.DependencyInjection;
using Composable.System.Configuration;

namespace Composable.Messaging.Buses.Implementation
{
    public class EndpointConfiguration
    {
        internal readonly IRunMode Mode;

        string ConfigurationParameterName(string name) => $"HostedEndpoint.{Name}.{name}";

        internal string Name { get; }
        internal EndpointId Id { get; }
        internal string ConnectionStringName => ConfigurationParameterName("ConnectionString");
        internal bool IsPureClientEndpoint { get; }

        internal bool HasMessageHandlers => !IsPureClientEndpoint;


        internal EndpointConfiguration(string name, EndpointId id, IRunMode mode, bool isPureClientEndpoint)
        {
            Mode = mode;
            Name = name;
            Id = id;
            IsPureClientEndpoint = isPureClientEndpoint;
        }
    }

    class RealEndpointConfiguration
    {
        readonly EndpointConfiguration _conf;
        readonly IConfigurationParameterProvider _configurationParameterProvider;
        public RealEndpointConfiguration(EndpointConfiguration conf, IConfigurationParameterProvider configurationParameterProvider)
        {
            _conf = conf;
            _configurationParameterProvider = configurationParameterProvider;
        }

        internal string Address
        {
            get
            {
                if(_conf.Mode.IsTesting)
                {
                    return "tcp://localhost:0";
                } else
                {
                    if(IsPureClientEndpoint)
                    {
                        return "invalid";
                    } else
                    {
                        return $"tcp://localhost:{EndpointConfigurationValue("Port")}";
                    }
                }
            }
        }

        internal string Name => _conf.Name;
        internal EndpointId Id => _conf.Id;
        internal bool IsPureClientEndpoint => _conf.IsPureClientEndpoint;

        string EndpointConfigurationValue(string name) => _configurationParameterProvider.GetString(ConfigurationParameterName(name)).Trim();
        string ConfigurationParameterName(string name) => $"HostedEndpoint.{Name}.{name}";
    }
}
