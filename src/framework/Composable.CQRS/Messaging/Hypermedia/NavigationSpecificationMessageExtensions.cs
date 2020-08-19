using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.Messaging.Hypermedia
{
    public static class NavigationSpecificationMessageExtensions
    {
        public static NavigationSpecification Post(this MessageTypes.Remotable.AtMostOnce.IAtMostOnceHypermediaCommand command) => NavigationSpecification.Post(command);

        public static NavigationSpecification<TResult> Post<TResult>(this MessageTypes.Remotable.AtMostOnce.IAtMostOnceCommand<TResult> command) => NavigationSpecification.Post(command);

        public static NavigationSpecification<TResult> Get<TResult>(this MessageTypes.Remotable.NonTransactional.IQuery<TResult> query) => NavigationSpecification.Get(query);


        public static TResult PostOn<TResult>(this MessageTypes.Remotable.AtMostOnce.IAtMostOnceCommand<TResult> command, IRemoteHypermediaNavigator bus) => NavigationSpecification.Post(command).NavigateOn(bus);

        public static TResult GetOn<TResult>(this MessageTypes.Remotable.NonTransactional.IQuery<TResult> query, IRemoteHypermediaNavigator bus) => NavigationSpecification.Get(query).NavigateOn(bus);

        public static TResult Navigate<TResult>(this IRemoteHypermediaNavigator navigator, NavigationSpecification<TResult> navigationSpecification) => navigationSpecification.NavigateOn(navigator);

        public static async Task<TResult> NavigateAsync<TResult>(this IRemoteHypermediaNavigator navigator, NavigationSpecification<TResult> navigationSpecification) => await navigationSpecification.NavigateOnAsync(navigator).NoMarshalling();
    }
}
