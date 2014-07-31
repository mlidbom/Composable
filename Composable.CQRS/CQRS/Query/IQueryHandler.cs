using System;

namespace Composable.CQRS.Query
{
    [Obsolete("Will be removed soon. Please switch to using another solution for queries.")]
    public interface IQueryHandler<in TQuery, out TQueryResult> where TQuery : IQuery<TQuery, TQueryResult>
    {
        TQueryResult Execute(TQuery query);
    }
}