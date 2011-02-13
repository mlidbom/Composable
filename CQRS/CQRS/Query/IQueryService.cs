namespace Composable.CQRS.Query
{
    public interface IQueryService
    {
        TQueryResult Execute<TQuery, TQueryResult>(IQuery<TQuery, TQueryResult> query) where TQuery : IQuery<TQuery, TQueryResult>;
    }
}