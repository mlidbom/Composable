
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public static class NavigationSpecificationMessageExtensions
    {
        public static NavigationSpecification<TResult> Post<TResult>(this IExactlyOnceCommand<TResult> command) => NavigationSpecification.Post(command);

        public static NavigationSpecification Post(this IExactlyOnceCommand command) => NavigationSpecification.Post(command);

        public static NavigationSpecification<TResult> Get<TResult>(this IQuery<TResult> query) => NavigationSpecification.Get(query);


        public static TResult PostOn<TResult>(this IExactlyOnceCommand<TResult> command, IServiceBusSession bus) => NavigationSpecification.Post(command).ExecuteOn(bus);

        public static void PostOn(this IExactlyOnceCommand command, IServiceBusSession bus) => NavigationSpecification.Post(command).ExecuteOn(bus);

        public static TResult GetOn<TResult>(this IQuery<TResult> query, IServiceBusSession bus) => NavigationSpecification.Get(query).ExecuteOn(bus);


        public static TResult ExecuteOn<TResult>(this IExactlyOnceCommand<TResult> command, ILocalServiceBusSession bus) => bus.Execute(command);

        public static void ExecuteOn(this IExactlyOnceCommand command, ILocalServiceBusSession bus) => bus.Execute(command);

        public static TResult ExecuteOn<TResult>(this IQuery<TResult> query, ILocalServiceBusSession bus) => bus.Execute(query);
    }
}
