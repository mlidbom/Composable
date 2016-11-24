namespace Composable.ServiceBus
{
    public interface IHandleMessages<TMessage> where TMessage : IMessage
    {
        void Handle(TMessage message);
    }
}