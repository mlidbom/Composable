namespace Composable.Messaging.Hypermedia
{
    ///<summary>Dispatches messages within a process.</summary>
    public interface ILocalHypermediaNavigator
    {
        ///<summary>Synchronously executes local handler for <paramref name="query"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
        TResult Execute<TQuery, TResult>(MessageTypes.IStrictlyLocalQuery<TQuery, TResult> query) where TQuery : MessageTypes.IStrictlyLocalQuery<TQuery, TResult>;

        ///<summary>Synchronously executes local handler for <paramref name="command"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
        TResult Execute<TResult>(MessageTypes.IStrictlyLocalCommand<TResult> command);

        ///<summary>Synchronously executes local handler for <paramref name="command"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
        void Execute(MessageTypes.IStrictlyLocalCommand command);
    }
}