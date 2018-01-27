
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public static class NavigationSpecificationMessageExtensions
    {
        public static RemoteNavigationSpecification<TResult> PostRemote<TResult>(this BusApi.Remote.ExactlyOnce.ICommand<TResult> command) => RemoteNavigationSpecification.PostRemote(command);

        public static RemoteNavigationSpecification PostRemote(this BusApi.Remote.ExactlyOnce.ICommand command) => RemoteNavigationSpecification.PostRemote(command);

        public static RemoteNavigationSpecification<TResult> GetRemote<TResult>(this BusApi.IQuery<TResult> query) => RemoteNavigationSpecification.GetRemote(query);


        public static TResult PostRemoteOn<TResult>(this BusApi.Remote.ExactlyOnce.ICommand<TResult> command, IRemoteServiceBusSession bus) => RemoteNavigationSpecification.PostRemote(command).ExecuteRemoteOn(bus);

        public static void PostRemoteOn(this BusApi.Remote.ExactlyOnce.ICommand command, IRemoteServiceBusSession bus) => RemoteNavigationSpecification.PostRemote(command).ExecuteRemoteOn(bus);

        public static TResult GetRemoteOn<TResult>(this BusApi.IQuery<TResult> query, IRemoteServiceBusSession bus) => RemoteNavigationSpecification.GetRemote(query).ExecuteRemoteOn(bus);


        public static TResult PostLocalOn<TResult>(this BusApi.Local.ICommand<TResult> command, ILocalServiceBusSession bus) => bus.PostLocal(command);

        public static void PostLocalOn(this BusApi.Local.ICommand command, ILocalServiceBusSession bus) => bus.PostLocal(command);

        public static TResult GetLocalOn<TResult>(this BusApi.Local.IQuery<TResult> query, ILocalServiceBusSession bus) => bus.GetLocal(query);
    }
}
