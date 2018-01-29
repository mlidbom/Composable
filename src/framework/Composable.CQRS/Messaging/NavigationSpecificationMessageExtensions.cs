
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public static class NavigationSpecificationMessageExtensions
    {
        public static RemoteNavigationSpecification PostRemote(this BusApi.Remote.AtMostOnce.ICommand command) => RemoteNavigationSpecification.PostRemote(command);

        public static RemoteNavigationSpecification<TResult> PostRemote<TResult>(this BusApi.Remote.AtMostOnce.ICommand<TResult> command) => RemoteNavigationSpecification.PostRemote(command);

        public static RemoteNavigationSpecification<TResult> GetRemote<TResult>(this BusApi.Remote.NonTransactional.IQuery<TResult> query) => RemoteNavigationSpecification.GetRemote(query);


        public static TResult PostRemoteOn<TResult>(this BusApi.Remote.AtMostOnce.ICommand<TResult> command, IUIInteractionApiBrowser bus) => RemoteNavigationSpecification.PostRemote(command).ExecuteRemoteOn(bus);

        public static TResult GetRemoteOn<TResult>(this BusApi.Remote.NonTransactional.IQuery<TResult> query, IUIInteractionApiBrowser bus) => RemoteNavigationSpecification.GetRemote(query).ExecuteRemoteOn(bus);


        public static TResult PostLocalOn<TResult>(this BusApi.Local.ICommand<TResult> command, ILocalApiBrowser bus) => bus.PostLocal(command);

        public static void PostLocalOn(this BusApi.Local.ICommand command, ILocalApiBrowser bus) => bus.PostLocal(command);

        public static TResult GetLocalOn<TResult>(this BusApi.Local.IQuery<TResult> query, ILocalApiBrowser bus) => bus.GetLocal(query);
    }
}
