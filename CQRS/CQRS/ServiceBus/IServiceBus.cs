namespace Composable.ServiceBus
{
    public interface IServiceBus
    {
        void Publish(object message);
        void SendLocal(object message);
        void Send(object message);
        void Reply(object message);
    }
}