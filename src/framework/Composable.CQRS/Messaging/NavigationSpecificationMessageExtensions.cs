
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public static class NavigationSpecificationMessageExtensions
    {
        public static RemoteNavigationSpecification<TResult> PostRemote<TResult>(this MessagingApi.Remote.ExactlyOnce.ICommand<TResult> command) => RemoteNavigationSpecification.PostRemote(command);

        public static RemoteNavigationSpecification PostRemote(this MessagingApi.Remote.ExactlyOnce.ICommand command) => RemoteNavigationSpecification.PostRemote(command);

        public static RemoteNavigationSpecification<TResult> GetRemote<TResult>(this MessagingApi.IQuery<TResult> query) => RemoteNavigationSpecification.GetRemote(query);


        public static TResult PostRemoteOn<TResult>(this MessagingApi.Remote.ExactlyOnce.ICommand<TResult> command, IRemoteServiceBusSession bus) => RemoteNavigationSpecification.PostRemote(command).ExecuteRemoteOn(bus);

        public static void PostRemoteOn(this MessagingApi.Remote.ExactlyOnce.ICommand command, IRemoteServiceBusSession bus) => RemoteNavigationSpecification.PostRemote(command).ExecuteRemoteOn(bus);

        public static TResult GetRemoteOn<TResult>(this MessagingApi.IQuery<TResult> query, IRemoteServiceBusSession bus) => RemoteNavigationSpecification.GetRemote(query).ExecuteRemoteOn(bus);


        public static TResult PostLocalOn<TResult>(this MessagingApi.Remote.ExactlyOnce.ICommand<TResult> command, ILocalServiceBusSession bus) => bus.PostLocal(command);

        public static void PostLocalOn(this MessagingApi.Remote.ExactlyOnce.ICommand command, ILocalServiceBusSession bus) => bus.PostLocal(command);

        public static TResult GetLocalOn<TResult>(this MessagingApi.IQuery<TResult> query, ILocalServiceBusSession bus) => bus.GetLocal(query);
    }
}
