using System.Threading.Tasks;

namespace Composable.Messaging.Hypermedia
{
    public interface IRemoteHypermediaNavigator
    {
        Task PostAsync(IAtMostOnceHypermediaCommand command);
        void Post(IAtMostOnceHypermediaCommand command);

        Task<TResult> PostAsync<TResult>(IAtMostOnceCommand<TResult> command);
        TResult Post<TResult>(IAtMostOnceCommand<TResult> command);

        ///<summary>Gets the result of a handler somewhere on the bus handling the <paramref name="query"/></summary>
        Task<TResult> GetAsync<TResult>(IRemotableQuery<TResult> query);

        ///<summary>Synchronous wrapper for: <see cref="GetAsync{TResult}"/>.</summary>
        TResult Get<TResult>(IRemotableQuery<TResult> query);
    }
}