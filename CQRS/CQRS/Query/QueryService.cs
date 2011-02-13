#region usings

using System;
using System.Transactions;
using Microsoft.Practices.ServiceLocation;

#endregion

namespace Composable.CQRS.Query
{
    public class QueryService : IQueryService
    {
        private readonly IServiceLocator _serviceLocator;

        public QueryService(IServiceLocator serviceLocator)
        {
            _serviceLocator = serviceLocator;
        }

        public TQueryResult Execute<TQuery, TQueryResult>(IQuery<TQuery, TQueryResult> query) where TQuery : IQuery<TQuery, TQueryResult>
        {
            using (var transaction = new TransactionScope())
            {
                var handler = _serviceLocator.GetInstance<IQueryHandler<TQuery, TQueryResult>>();
                var result = handler.Execute((TQuery) query);
                transaction.Complete();
                return result;
            }            
        }
    }
}