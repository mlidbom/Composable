namespace Composable.CQRS.ServiceBus.NServiceBus
{
    public class NullOpMessageInterceptor : IMessageInterceptor
    {
        public static readonly  NullOpMessageInterceptor Instance = new NullOpMessageInterceptor();
        public void BeforePublish()
        {}

        public void BeforeSend()
        {}

        public void BeforeSendLocal()
        {}

        private NullOpMessageInterceptor(){}
    }
}