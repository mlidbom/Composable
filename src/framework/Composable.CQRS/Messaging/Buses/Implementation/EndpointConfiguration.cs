namespace Composable.Messaging.Buses.Implementation
{
    class EndpointConfiguration
    {
        internal string Address { get; }
        public EndpointConfiguration()
        {
            Address = "tcp://localhost:0";
        }
    }
}
