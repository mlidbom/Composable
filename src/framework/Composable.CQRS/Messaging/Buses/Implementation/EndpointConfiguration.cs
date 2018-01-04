namespace Composable.Messaging.Buses.Implementation
{
    public class EndpointConfiguration
    {
        internal string Address { get; }
        public string Name { get; }
        public string ConnectionStringName => Name;

        internal EndpointConfiguration(string name)
        {
            Name = name;
            Address = "tcp://localhost:0";
        }
    }
}
