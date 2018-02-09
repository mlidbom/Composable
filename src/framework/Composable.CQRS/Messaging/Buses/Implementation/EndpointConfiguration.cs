namespace Composable.Messaging.Buses.Implementation
{
    public class EndpointConfiguration
    {
        internal string Address { get; }
        public string Name { get; }
        public EndpointId Id { get; }
        public string ConnectionStringName => Name;

        internal EndpointConfiguration(string name, EndpointId id)
        {
            Name = name;
            Id = id;
            Address = "tcp://localhost:0";
        }
    }
}
