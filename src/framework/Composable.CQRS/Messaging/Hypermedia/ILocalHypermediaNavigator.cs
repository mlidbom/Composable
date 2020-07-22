namespace Composable.Messaging.Hypermedia
{
    ///<summary>Dispatches messages within a process.</summary>
    public interface ILocalHypermediaNavigator
    {
        ///<summary>Synchronously executes local handler for <paramref name="query"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
        TResult Execute<TQuery, TResult>(MessageTypes.StrictlyLocal.IQuery<TQuery, TResult> query) where TQuery : MessageTypes.StrictlyLocal.IQuery<TQuery, TResult>;

        ///<summary>Synchronously executes local handler for <paramref name="command"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
        TResult Execute<TResult>(MessageTypes.StrictlyLocal.ICommand<TResult> command);

        ///<summary>Synchronously executes local handler for <paramref name="command"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
        void Execute(MessageTypes.StrictlyLocal.ICommand command);
    }
}