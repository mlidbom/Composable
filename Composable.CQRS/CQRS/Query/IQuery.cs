using System;

namespace Composable.CQRS.Query
{
    [Obsolete("Will be removed soon. Please switch to using another solution for queries.")]
    public interface IQuery<TQuery, TQueryResult> where TQuery : IQuery<TQuery, TQueryResult>
    {
    }
}