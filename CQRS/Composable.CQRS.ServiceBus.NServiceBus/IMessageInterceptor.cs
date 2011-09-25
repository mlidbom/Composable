namespace Composable.CQRS.ServiceBus.NServiceBus
{
    public interface IMessageInterceptor
    {
        void BeforePublish(object message);
        void BeforeSend(object message);
        void BeforeSendLocal(object message);
    }
}