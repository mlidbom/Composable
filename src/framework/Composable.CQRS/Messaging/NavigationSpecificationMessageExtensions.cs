
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
    }
}
