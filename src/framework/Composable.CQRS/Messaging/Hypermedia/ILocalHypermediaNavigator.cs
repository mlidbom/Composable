namespace Composable.Messaging.Hypermedia
{
    ///<summary>Dispatches messages within a process.</summary>
    public interface ILocalHypermediaNavigator
    {
        ///<summary>Synchronously executes local handler for <paramref name="query"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
        TResult Execute<TQuery, TResult>(IStrictlyLocalQuery<TQuery, TResult> query) where TQuery : IStrictlyLocalQuery<TQuery, TResult>;

        ///<summary>Synchronously executes local handler for <paramref name="command"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
        TResult Execute<TResult>(IStrictlyLocalCommand<TResult> command);

        ///<summary>Synchronously executes local handler for <paramref name="command"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
        void Execute(IStrictlyLocalCommand command);
    }
}