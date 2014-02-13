#region usings

using System;
using System.Diagnostics.Contracts;

#endregion

namespace Composable.CQRS.Query
{
    [ContractClass(typeof(QUeryServiceContract))]
    public interface IQueryService
    {
        TQueryResult Execute<TQuery, TQueryResult>(IQuery<TQuery, TQueryResult> query) where TQuery : IQuery<TQuery, TQueryResult>;
    }

    [ContractClassFor(typeof(IQueryService))]
    internal abstract class QUeryServiceContract : IQueryService
    {
        public TQueryResult Execute<TQuery, TQueryResult>(IQuery<TQuery, TQueryResult> query) where TQuery : IQuery<TQuery, TQueryResult>
        {
            Contract.Requires(query != null);
            return default(TQueryResult);
        }
    }
}