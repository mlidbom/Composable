namespace Composable.Messaging.Buses
{
    public static class ServiceBusSynchronousExtensions
    {
        public static void Publish(this IServiceBus @this, IEvent @event) => @this.PublishAsync(@event).Wait();
        public static void Send(this IServiceBus @this, IDomainCommand command) => @this.SendAsync(command).Wait();
        public static TResult Send<TResult>(this IServiceBus @this, IDomainCommand<TResult> command) => @this.SendAsync(command).Result;
        public static TResult Query<TResult>(this IServiceBus @this, IQuery<TResult> query) => @this.QueryAsync(query).Result;
    }

    public static class ApiNavigatorSynchronousExtensions
    {
        public static TResult Execute<TResult>(this IApiNavigator<TResult> @this) => @this.ExecuteAsync().Result;
    }
}
