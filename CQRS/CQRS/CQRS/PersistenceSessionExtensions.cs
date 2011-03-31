#region usings

using System.Diagnostics.Contracts;
using Composable.Data.ORM;

#endregion

namespace Composable.CQRS
{
    public static class PersistenceSessionExtensions
    {
        public static IEntityFetcher AsEntityFetcher(this IPersistenceSession me)
        {
            Contract.Requires(me != null);
            return new PersistenceSessionEntityFetcherAdapter(me);
        }

        private class PersistenceSessionEntityFetcherAdapter : IEntityFetcher
        {
            private readonly IPersistenceSession _session;

            public PersistenceSessionEntityFetcherAdapter(IPersistenceSession session)
            {
                Contract.Requires(session != null);
                _session = session;
            }

            [ContractInvariantMethod]
            private void Invariants()
            {
                Contract.Invariant(_session != null);
            }

            public TEntity Get<TEntity>(object entityId)
            {
                return _session.Get<TEntity>(entityId);
            }
        }
    }
}