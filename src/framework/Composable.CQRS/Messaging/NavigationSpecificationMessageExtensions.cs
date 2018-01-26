
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public static class NavigationSpecificationMessageExtensions
    {
        public static RemoteNavigationSpecification<TResult> PostRemote<TResult>(this IExactlyOnceCommand<TResult> command) => RemoteNavigationSpecification.PostRemote(command);

        public static RemoteNavigationSpecification PostRemote(this IExactlyOnceCommand command) => RemoteNavigationSpecification.PostRemote(command);

        public static RemoteNavigationSpecification<TResult> GetRemote<TResult>(this IQuery<TResult> query) => RemoteNavigationSpecification.GetRemote(query);


        public static TResult PostRemoteOn<TResult>(this IExactlyOnceCommand<TResult> command, IRemoteServiceBusSession bus) => RemoteNavigationSpecification.PostRemote(command).ExecuteRemoteOn(bus);

        public static void PostRemoteOn(this IExactlyOnceCommand command, IRemoteServiceBusSession bus) => RemoteNavigationSpecification.PostRemote(command).ExecuteRemoteOn(bus);

        public static TResult GetRemoteOn<TResult>(this IQuery<TResult> query, IRemoteServiceBusSession bus) => RemoteNavigationSpecification.GetRemote(query).ExecuteRemoteOn(bus);


        public static TResult PostLocalOn<TResult>(this IExactlyOnceCommand<TResult> command, ILocalServiceBusSession bus) => bus.PostLocal(command);

        public static void PostLocalOn(this IExactlyOnceCommand command, ILocalServiceBusSession bus) => bus.PostLocal(command);

        public static TResult GetLocalOn<TResult>(this IQuery<TResult> query, ILocalServiceBusSession bus) => bus.GetLocal(query);
    }
}
