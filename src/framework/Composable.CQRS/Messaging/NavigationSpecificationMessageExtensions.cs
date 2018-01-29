
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public static class NavigationSpecificationMessageExtensions
    {
        public static NavigationSpecification Post(this BusApi.RemoteSupport.AtMostOnce.ICommand command) => NavigationSpecification.Post(command);

        public static NavigationSpecification<TResult> Post<TResult>(this BusApi.RemoteSupport.AtMostOnce.ICommand<TResult> command) => NavigationSpecification.Post(command);

        public static NavigationSpecification<TResult> Get<TResult>(this BusApi.RemoteSupport.NonTransactional.IQuery<TResult> query) => NavigationSpecification.Get(query);


        public static TResult PostOn<TResult>(this BusApi.RemoteSupport.AtMostOnce.ICommand<TResult> command, IRemoteApiNavigatorSession bus) => NavigationSpecification.Post(command).NavigateOn(bus);

        public static TResult GetOn<TResult>(this BusApi.RemoteSupport.NonTransactional.IQuery<TResult> query, IRemoteApiNavigatorSession bus) => NavigationSpecification.Get(query).NavigateOn(bus);
    }
}
