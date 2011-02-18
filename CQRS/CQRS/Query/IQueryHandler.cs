namespace Composable.CQRS.Query
{
    public interface IQueryHandler<in TQuery, out TQueryResult> where TQuery : IQuery<TQuery, TQueryResult>
    {
        TQueryResult Execute(TQuery query);
    }
}