namespace Composable.CQRS.ServiceBus.NServiceBus
{
    public interface IMessageInterceptor
    {
        void BeforePublish();
        void BeforeSend();
        void BeforeSendLocal();
    }
}