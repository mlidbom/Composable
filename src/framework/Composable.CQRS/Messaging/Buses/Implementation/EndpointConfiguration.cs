using Composable.DependencyInjection;
using Composable.System.Configuration;

namespace Composable.Messaging.Buses.Implementation
{
    public class EndpointConfiguration
    {
        internal string Address { get; }
        public string Name { get; }
        public EndpointId Id { get; }
        public string ConnectionStringName => Name;
        internal bool IsPureClientEndpoint { get; set; }

        internal bool HasMessageHandlers => !IsPureClientEndpoint;

        internal EndpointConfiguration(string name, EndpointId id, IRunMode mode, bool isPureClientEndpoint)
        {
            Name = name;
            Id = id;
            IsPureClientEndpoint = isPureClientEndpoint;

            if(mode.IsTesting)
            {
                Address = "tcp://localhost:0";
            } else
            {
                if(isPureClientEndpoint)
                {
                    Address = "invalid";
                } else
                {
                    var port = new AppConfigConfigurationParameterProvider().GetString($"HostedEndpoint.{Name}.Port").Trim();
                    Address = $"tcp://localhost:{port}";
                }
            }
        }
    }
}
