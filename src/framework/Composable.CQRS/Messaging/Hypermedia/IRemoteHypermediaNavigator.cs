using System.Threading.Tasks;

namespace Composable.Messaging.Hypermedia
{
    public interface IRemoteHypermediaNavigator
    {
        Task PostAsync(MessageTypes.IAtMostOnceHypermediaCommand command);
        void Post(MessageTypes.IAtMostOnceHypermediaCommand command);

        Task<TResult> PostAsync<TResult>(MessageTypes.IAtMostOnceCommand<TResult> command);
        TResult Post<TResult>(MessageTypes.IAtMostOnceCommand<TResult> command);

        ///<summary>Gets the result of a handler somewhere on the bus handling the <paramref name="query"/></summary>
        Task<TResult> GetAsync<TResult>(MessageTypes.IRemotableQuery<TResult> query);

        ///<summary>Synchronous wrapper for: <see cref="GetAsync{TResult}"/>.</summary>
        TResult Get<TResult>(MessageTypes.IRemotableQuery<TResult> query);
    }
}