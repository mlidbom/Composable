namespace Composable.CQRS.ServiceBus.NServiceBus
{
    public class EmptyMessageInterceptor : IMessageInterceptor
    {
        public static readonly  EmptyMessageInterceptor Instance = new EmptyMessageInterceptor();

        private EmptyMessageInterceptor(){}
        public virtual void BeforePublish(object message)
        {}

        public virtual void BeforeSend(object message)
        {}

        public virtual void BeforeSendLocal(object message)
        {}
    }
}