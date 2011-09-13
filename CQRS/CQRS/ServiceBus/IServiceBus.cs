namespace Composable.ServiceBus
{
    public interface IServiceBus
    {
        void Publish(object message);
        void SendLocal(object message);
    }
}