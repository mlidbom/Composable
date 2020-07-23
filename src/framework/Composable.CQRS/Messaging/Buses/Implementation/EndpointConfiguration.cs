using Composable.DependencyInjection;
using Composable.SystemCE.ConfigurationCE;

namespace Composable.Messaging.Buses.Implementation
{
    public class EndpointConfiguration
    {
        internal readonly IRunMode Mode;

        internal string Name { get; }
        internal EndpointId Id { get; }
        internal string ConnectionStringName { get; }
        internal bool IsPureClientEndpoint { get; }


        internal EndpointConfiguration(string name, EndpointId id, IRunMode mode, bool isPureClientEndpoint)
        {
            Mode = mode;
            Name = name;
            Id = id;
            IsPureClientEndpoint = isPureClientEndpoint;
            ConnectionStringName = $"HostedEndpoint.{Name}.ConnectionString";
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

            if(_conf.Mode.IsTesting)
            {
                Address = "tcp://localhost:0";
            } else
            {
                if(IsPureClientEndpoint)
                {
                    Address = "invalid";
                } else
                {
                    Address = $"tcp://localhost:{EndpointConfigurationValue("Port")}";
                }
            }
        }

        internal string Address { get; }

        internal string Name => _conf.Name;
        internal EndpointId Id => _conf.Id;
        internal bool IsPureClientEndpoint => _conf.IsPureClientEndpoint;

        string EndpointConfigurationValue(string name) => _configurationParameterProvider.GetString(ConfigurationParameterName(name)).Trim();
        string ConfigurationParameterName(string name) => $"HostedEndpoint.{Name}.{name}";
    }
}
