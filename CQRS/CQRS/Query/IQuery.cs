namespace Composable.CQRS.Query
{
    public interface IQuery<TQuery, TQueryResult> where TQuery : IQuery<TQuery, TQueryResult>
    {
    }
}