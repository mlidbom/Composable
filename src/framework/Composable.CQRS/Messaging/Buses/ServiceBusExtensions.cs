namespace Composable.Messaging.Buses
{
    public static class ServiceBusExtensions
    {
        public static IApiNavigator<TReturnResource> Get<TReturnResource>(this IServiceBus @this, IQuery<TReturnResource> createQuery)
            => new ApiNavigator<TReturnResource>(@this, () => @this.QueryAsync(createQuery));

        public static IApiNavigator<TCommandResult> Post<TCommandResult>(this IServiceBus @this, IDomainCommand<TCommandResult> command)
            => new ApiNavigator<TCommandResult>(@this, () => @this.SendAsync(command));
    }
}
