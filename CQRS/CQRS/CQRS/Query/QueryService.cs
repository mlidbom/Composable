#region usings

using System.Diagnostics.Contracts;
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
            Contract.Requires(serviceLocator != null);
            _serviceLocator = serviceLocator;
        }

        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_serviceLocator != null);
        }

        public TQueryResult Execute<TQuery, TQueryResult>(IQuery<TQuery, TQueryResult> query) where TQuery : IQuery<TQuery, TQueryResult>
        {
            //Todo: perhaps we do need a transaction here. But for now It's out since it causes new and funny transaction problems with IntegrationTests in CVManagement
            //using(var transaction = new TransactionScope())
            //{
                var handler = _serviceLocator.GetSingleInstance<IQueryHandler<TQuery, TQueryResult>>();
                var result = handler.Execute((TQuery)query);
              //  transaction.Complete();
                return result;
            //}
        }
    }
}