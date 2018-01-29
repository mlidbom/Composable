
using System.Threading.Tasks;
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public static class NavigationSpecificationMessageExtensions
    {
        public static NavigationSpecification Post(this BusApi.Remotable.AtMostOnce.ICommand command) => NavigationSpecification.Post(command);

        public static NavigationSpecification<TResult> Post<TResult>(this BusApi.Remotable.AtMostOnce.ICommand<TResult> command) => NavigationSpecification.Post(command);

        public static NavigationSpecification<TResult> Get<TResult>(this BusApi.Remotable.NonTransactional.IQuery<TResult> query) => NavigationSpecification.Get(query);


        public static TResult PostOn<TResult>(this BusApi.Remotable.AtMostOnce.ICommand<TResult> command, IRemoteApiNavigatorSession bus) => NavigationSpecification.Post(command).NavigateOn(bus);

        public static TResult GetOn<TResult>(this BusApi.Remotable.NonTransactional.IQuery<TResult> query, IRemoteApiNavigatorSession bus) => NavigationSpecification.Get(query).NavigateOn(bus);

        public static TResult Navigate<TResult>(this IRemoteApiNavigatorSession navigator, NavigationSpecification<TResult> navigationSpecification) => navigationSpecification.NavigateOn(navigator);

        public static async Task<TResult> NavigateAsync<TResult>(this IRemoteApiNavigatorSession navigator, NavigationSpecification<TResult> navigationSpecification) => await navigationSpecification.NavigateOnAsync(navigator);
    }
}
