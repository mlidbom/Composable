
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public static class NavigationSpecificationMessageExtensions
    {
        public static RemoteNavigationSpecification Post(this BusApi.RemoteSupport.AtMostOnce.ICommand command) => RemoteNavigationSpecification.Post(command);

        public static RemoteNavigationSpecification<TResult> Post<TResult>(this BusApi.RemoteSupport.AtMostOnce.ICommand<TResult> command) => RemoteNavigationSpecification.Post(command);

        public static RemoteNavigationSpecification<TResult> Get<TResult>(this BusApi.RemoteSupport.NonTransactional.IQuery<TResult> query) => RemoteNavigationSpecification.Get(query);


        public static TResult PostOn<TResult>(this BusApi.RemoteSupport.AtMostOnce.ICommand<TResult> command, IRemoteApiBrowser bus) => RemoteNavigationSpecification.Post(command).NavigateOn(bus);

        public static TResult GetOn<TResult>(this BusApi.RemoteSupport.NonTransactional.IQuery<TResult> query, IRemoteApiBrowser bus) => RemoteNavigationSpecification.Get(query).NavigateOn(bus);


        public static TResult ExecuteOn<TResult>(this BusApi.StrictlyLocal.ICommand<TResult> command, ILocalApiBrowser bus) => bus.Execute(command);

        public static void ExecuteOn(this BusApi.StrictlyLocal.ICommand command, ILocalApiBrowser bus) => bus.Execute(command);

        public static TResult ExecuteOn<TResult>(this BusApi.StrictlyLocal.IQuery<TResult> query, ILocalApiBrowser bus) => bus.Execute(query);
    }
}
