using Composable.DependencyInjection;
using Composable.System.Configuration;

namespace Composable.Messaging.Buses.Implementation
{
    public class EndpointConfiguration
    {
        readonly IRunMode _mode;
        IConfigurationParameterProvider _configurationParameterProvider;

        internal string Address
        {
            get
            {
                if(_mode.IsTesting)
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

        string EndpointConfigurationValue(string name) => _configurationParameterProvider.GetString(ConfigurationParameterName(name)).Trim();
        string ConfigurationParameterName(string name) => $"HostedEndpoint.{Name}.{name}";

        public string Name { get; }
        public EndpointId Id { get; }
        public string ConnectionStringName => ConfigurationParameterName("ConnectionString");
        internal bool IsPureClientEndpoint { get; }

        internal bool HasMessageHandlers => !IsPureClientEndpoint;

        //Review:mlidbo: This is not pretty. Find a better way than a magic init method that has to be called at a magic moment.
        internal void Init(IConfigurationParameterProvider configurationParameterProvider) { _configurationParameterProvider = configurationParameterProvider; }

        internal EndpointConfiguration(string name, EndpointId id, IRunMode mode, bool isPureClientEndpoint)
        {
            _mode = mode;
            Name = name;
            Id = id;
            IsPureClientEndpoint = isPureClientEndpoint;
        }
    }
}
