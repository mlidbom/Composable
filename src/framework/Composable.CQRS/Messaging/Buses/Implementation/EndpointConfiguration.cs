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

        internal EndpointConfiguration(string name, EndpointId id)
        {
            Name = name;
            Id = id;
            Address = "tcp://localhost:0";
        }
    }
}
