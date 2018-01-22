
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public static class NavigationSpecificationMessageExtensions
    {
        public static NavigationSpecification<TResult> PostRemote<TResult>(this ITransactionalExactlyOnceDeliveryCommand<TResult> command) => NavigationSpecification.PostRemote(command);
        public static NavigationSpecification<TResult> Post<TResult>(this ITransactionalExactlyOnceDeliveryCommand<TResult> command) => NavigationSpecification.Post(command);

        public static NavigationSpecification PostRemote(this ITransactionalExactlyOnceDeliveryCommand command) => NavigationSpecification.PostRemote(command);
        public static NavigationSpecification Post(this ITransactionalExactlyOnceDeliveryCommand command) => NavigationSpecification.Post(command);

        public static NavigationSpecification<TResult> GetRemote<TResult>(this IQuery<TResult> query) => NavigationSpecification.GetRemote(query);
        public static NavigationSpecification<TResult> Get<TResult>(this IQuery<TResult> query) => NavigationSpecification.Get(query);


        public static TResult PostRemoteOn<TResult>(this ITransactionalExactlyOnceDeliveryCommand<TResult> command, IServiceBusSession bus) => NavigationSpecification.PostRemote(command).ExecuteOn(bus);
        public static TResult PostOn<TResult>(this ITransactionalExactlyOnceDeliveryCommand<TResult> command, IServiceBusSession bus) => NavigationSpecification.Post(command).ExecuteOn(bus);

        public static void PostRemoteOn(this ITransactionalExactlyOnceDeliveryCommand command, IServiceBusSession bus) => NavigationSpecification.PostRemote(command).ExecuteOn(bus);
        public static void PostOn(this ITransactionalExactlyOnceDeliveryCommand command, IServiceBusSession bus) => NavigationSpecification.Post(command).ExecuteOn(bus);

        public static TResult GetRemoteOn<TResult>(this IQuery<TResult> query, IServiceBusSession bus) => NavigationSpecification.GetRemote(query).ExecuteOn(bus);
        public static TResult GetOn<TResult>(this IQuery<TResult> query, IServiceBusSession bus) => NavigationSpecification.Get(query).ExecuteOn(bus);
    }
}
