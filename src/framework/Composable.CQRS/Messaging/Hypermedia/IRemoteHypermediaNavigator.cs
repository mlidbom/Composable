using System.Threading.Tasks;

namespace Composable.Messaging.Hypermedia
{
    public interface IRemoteHypermediaNavigator
    {
        Task PostAsync(MessageTypes.Remotable.AtMostOnce.IAtMostOnceHypermediaCommand command);
        void Post(MessageTypes.Remotable.AtMostOnce.IAtMostOnceHypermediaCommand command);

        Task<TResult> PostAsync<TResult>(MessageTypes.Remotable.AtMostOnce.IAtMostOnceCommand<TResult> command);
        TResult Post<TResult>(MessageTypes.Remotable.AtMostOnce.IAtMostOnceCommand<TResult> command);

        ///<summary>Gets the result of a handler somewhere on the bus handling the <paramref name="query"/></summary>
        Task<TResult> GetAsync<TResult>(MessageTypes.Remotable.NonTransactional.IQuery<TResult> query);

        ///<summary>Synchronous wrapper for: <see cref="GetAsync{TResult}"/>.</summary>
        TResult Get<TResult>(MessageTypes.Remotable.NonTransactional.IQuery<TResult> query);
    }
}