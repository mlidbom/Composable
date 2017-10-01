namespace Composable.Messaging.Buses
{
    static class ServiceBusExtensions
    {
        public static IApiNavigator<TReturnResource> Get<TReturnResource>(this IServiceBus @this, IQuery<TReturnResource> createQuery) where TReturnResource : IQueryResult
            => new ApiNavigator<TReturnResource>(@this, () => @this.QueryAsync(createQuery));

        public static IApiNavigator<TCommandResult> Post<TCommandResult>(this IServiceBus @this, ICommand<TCommandResult> command) where TCommandResult : IMessage
            => new ApiNavigator<TCommandResult>(@this, () => @this.SendAsync(command));
    }
}
